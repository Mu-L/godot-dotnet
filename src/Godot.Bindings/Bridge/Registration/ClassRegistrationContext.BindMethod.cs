using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Godot.NativeInterop;

namespace Godot.Bridge;

partial class ClassRegistrationContext
{
    private readonly HashSet<StringName> _registeredMethods = new(StringNameEqualityComparer.Default);

    private readonly Dictionary<StringName, GCHandle> _registeredMethodHandles = [];

    // The MethodDefinition must be referenced somewhere so the GC doesn't release it.
    // We need to keep it alive because it contains the MethodBindInvoker that
    // invokes the method in the 'call_func' and 'ptrcall_func' callbacks.
    private readonly Dictionary<StringName, MethodDefinition> _registeredMethodImplementations = new(StringNameEqualityComparer.Default);

    /// <summary>
    /// Register a method in the class.
    /// </summary>
    /// <param name="methodDefinition">Information that describes the method to register.</param>
    /// <exception cref="ArgumentException">
    /// A method has already been registered with the same name.
    /// </exception>
    public unsafe void BindMethod(MethodDefinition methodDefinition)
    {
        if (!_registeredMethods.Add(methodDefinition.Name))
        {
            throw new ArgumentException(SR.FormatArgument_MethodAlreadyRegistered(methodDefinition.Name, ClassName), nameof(methodDefinition));
        }

        _registeredMethodImplementations[methodDefinition.Name] = methodDefinition;

        _registerBindingActions.Enqueue(() =>
        {
            // Convert managed method info to the internal unmanaged type.
            var methodInfoNative = new GDExtensionClassMethodInfo();
            {
                NativeGodotStringName nameNative = methodDefinition.Name.NativeValue.DangerousSelfRef;
                methodInfoNative.name = &nameNative;

                var methodFlags = GDExtensionClassMethodFlags.GDEXTENSION_METHOD_FLAGS_DEFAULT;
                if (methodDefinition.IsStatic)
                {
                    methodFlags |= GDExtensionClassMethodFlags.GDEXTENSION_METHOD_FLAG_STATIC;
                }
                methodInfoNative.method_flags = (uint)methodFlags;

                // Return

                if (methodDefinition.Return is not null)
                {
                    // Convert managed property info to the internal unmanaged type.
                    GDExtensionPropertyInfo ret;
                    {
                        NativeGodotStringName returnNameNative = methodDefinition.Return.Name.NativeValue.DangerousSelfRef;
                        NativeGodotStringName returnClassNameNative = (methodDefinition.Return.ClassName?.NativeValue ?? default).DangerousSelfRef;
                        NativeGodotString hintStringNative = NativeGodotString.Create(methodDefinition.Return.HintString);

                        ret = new GDExtensionPropertyInfo()
                        {
                            type = (GDExtensionVariantType)methodDefinition.Return.Type,
                            name = &returnNameNative,

                            hint = (uint)methodDefinition.Return.Hint,
                            hint_string = &hintStringNative,
                            class_name = &returnClassNameNative,
                            usage = (uint)methodDefinition.Return.Usage,
                        };
                    }

                    methodInfoNative.has_return_value = true;
                    methodInfoNative.return_value_info = &ret;
                    methodInfoNative.return_value_metadata = (GDExtensionClassMethodArgumentMetadata)methodDefinition.Return.TypeMetadata;
                }

                // Parameters

                var args = stackalloc GDExtensionPropertyInfo[methodDefinition.Parameters.Count];
                var argsMetadata = stackalloc GDExtensionClassMethodArgumentMetadata[methodDefinition.Parameters.Count];
                var argsDefaultValues = stackalloc NativeGodotVariant*[methodDefinition.Parameters.Count];

                uint optionalParameterCount = 0;
                for (int i = 0; i < methodDefinition.Parameters.Count; i++)
                {
                    var parameter = methodDefinition.Parameters[i];

                    if (optionalParameterCount > 0 && parameter.DefaultValue is null)
                    {
                        throw new InvalidOperationException(SR.InvalidOperation_MethodOptionalParametersMustAppearAfterRequiredParameters);
                    }

                    if (parameter.DefaultValue is not null)
                    {
                        NativeGodotVariant defaultValue = parameter.DefaultValue.Value.NativeValue.DangerousSelfRef;
                        argsDefaultValues[optionalParameterCount++] = &defaultValue;
                    }

                    // Convert managed parameter info to the internal unmanaged type.
                    {
                        NativeGodotStringName parameterNameNative = parameter.Name.NativeValue.DangerousSelfRef;
                        NativeGodotStringName parameterClassNameNative = (parameter.ClassName?.NativeValue ?? default).DangerousSelfRef;
                        NativeGodotString hintStringNative = NativeGodotString.Create(parameter.HintString);

                        args[i] = new GDExtensionPropertyInfo()
                        {
                            type = (GDExtensionVariantType)parameter.Type,
                            name = &parameterNameNative,

                            hint = (uint)parameter.Hint,
                            hint_string = &hintStringNative,
                            class_name = &parameterClassNameNative,
                            usage = (uint)parameter.Usage,
                        };
                    }
                    argsMetadata[i] = (GDExtensionClassMethodArgumentMetadata)parameter.TypeMetadata;
                }

                methodInfoNative.argument_count = (uint)methodDefinition.Parameters.Count;
                methodInfoNative.arguments_info = args;
                methodInfoNative.arguments_metadata = argsMetadata;

                methodInfoNative.default_argument_count = optionalParameterCount;
                methodInfoNative.default_arguments = argsDefaultValues;
            }

            var methodGCHandle = GCHandle.Alloc(methodDefinition, GCHandleType.Normal);
            _registeredMethodHandles.Add(methodDefinition.Name, methodGCHandle);

            nint methodDefinitionPtr = GCHandle.ToIntPtr(methodGCHandle);
            methodInfoNative.call_func = &CallWithVariantArgs_Native;
            methodInfoNative.ptrcall_func = &CallWithPtrArgs_Native;
            methodInfoNative.method_userdata = (void*)methodDefinitionPtr;

            NativeGodotStringName classNameNative = ClassName.NativeValue.DangerousSelfRef;

            GodotBridge.GDExtensionInterface.classdb_register_extension_class_method(GodotBridge.LibraryPtr, &classNameNative, &methodInfoNative);
        });
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void CallWithPtrArgs_Native(void* methodUserData, void* instance, void** args, void* outRet)
    {
        try
        {
            var gcHandle = GCHandle.FromIntPtr((nint)methodUserData);
            var method = (MethodDefinition?)gcHandle.Target;

            Debug.Assert(method is not null);

            method.Invoker.CallWithPtrArgs(method, instance, args, outRet);
        }
        catch (Exception exception) when (ExceptionHandling.IsHandled(exception)) { }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static unsafe void CallWithVariantArgs_Native(void* methodUserData, void* instance, NativeGodotVariant** args, long argCount, NativeGodotVariant* outRet, GDExtensionCallError* outError)
    {
        try
        {
            var gcHandle = GCHandle.FromIntPtr((nint)methodUserData);
            var method = (MethodDefinition?)gcHandle.Target;

            Debug.Assert(method is not null);

            method.Invoker.CallWithVariantArgs(method, instance, new NativeGodotVariantPtrSpan(args, (int)argCount), outRet, outError);
        }
        catch (Exception exception) when (ExceptionHandling.IsHandled(exception))
        {
            outError->error = GDExtensionCallErrorType.GDEXTENSION_CALL_ERROR_INVALID_METHOD;
        }
    }
}
