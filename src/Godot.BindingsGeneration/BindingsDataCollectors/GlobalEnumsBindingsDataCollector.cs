using System;
using System.Collections.Generic;
using Godot.BindingsGeneration.Reflection;
using Godot.Common;

namespace Godot.BindingsGeneration;

internal sealed class GlobalEnumsBindingsDataCollector : BindingsDataCollector
{
    public override void Initialize(BindingsData.CollectionContext context)
    {
        foreach (var globalEnum in context.Api.GlobalEnums)
        {
            var @enum = new EnumInfo(NamingUtils.PascalToPascalCase(globalEnum.Name), context.Options.Namespace)
            {
                VisibilityAttributes = VisibilityAttributes.Public,
                HasFlagsAttribute = globalEnum.IsBitField,
                UnderlyingType = KnownTypes.SystemInt64,
            };

            context.AddGeneratedType($"GlobalEnums/{@enum.Name}.cs", @enum);
            context.TypeDB.RegisterTypeName(globalEnum.Name, @enum);

            // IMPORTANT: In the engine, all global enums are declared in the '@GlobalScope' type,
            // so we need to register the enum type as a global member so it can be resolved from
            // the documentation.
            context.TypeDB.RegisterGlobalMemberMapping($"@GlobalScope.{globalEnum.Name}", @enum.FullNameWithGlobal);
        }
    }

    public override void Populate(BindingsData.CollectionContext context)
    {
        foreach (var globalEnum in context.Api.GlobalEnums)
        {
            var type = context.TypeDB.GetTypeFromEngineName(globalEnum.Name);

            if (type is not EnumInfo enumType)
            {
                throw new InvalidOperationException($"Type found for '{globalEnum.Name}' is not an enum.");
            }

            Dictionary<FieldInfo, string> enumMemberMappings = [];
            List<FieldInfo> enumConstantsByIndex = [];
            foreach (var engineConstant in globalEnum.Values)
            {
                var constant = new FieldInfo(NamingUtils.SnakeToPascalCase(engineConstant.Name), enumType.UnderlyingType ?? KnownTypes.SystemInt32)
                {
                    VisibilityAttributes = VisibilityAttributes.Public,
                    IsLiteral = true,
                    DefaultValue = $"{engineConstant.Value}",
                    Documentation = context.Options.IncludeDocumentation ? engineConstant.Description : null,
                };

                type.DeclaredFields.Add(constant);
                enumMemberMappings[constant] = engineConstant.Name;
                enumConstantsByIndex.Add(constant);
            }

            int enumPrefix = NamingUtils.DetermineEnumPrefix(globalEnum);

            // HARDCODED: The Error enum have the prefix 'ERR_' for everything except 'OK' and 'FAILED'.
            if (type.ContainingType is null && type.Name == "Error")
            {
                if (enumPrefix > 0)
                {
                    // Just in case it ever changes.
                    throw new InvalidOperationException($"Prefix for enum 'Error' is not empty.");
                }

                enumPrefix = 1; // 'ERR_'
            }

            NamingUtils.ApplyPrefixToEnumConstants(globalEnum, enumType, enumPrefix);
            NamingUtils.RemoveMaxConstant(globalEnum, enumType, enumConstantsByIndex);

            // HARDCODED: Some enums have more constants that should be removed.
            if (globalEnum.Name is "JoyButton" or "JoyAxis")
            {
                NamingUtils.RemoveConstant(globalEnum, enumType, enumConstantsByIndex,
                    engineConstant => engineConstant.Name.EndsWith("_SDL_MAX", StringComparison.Ordinal));
            }

            // IMPORTANT: We only register the enum member mappings after we have processed the enum constants.
            foreach (var constant in enumType.DeclaredFields)
            {
                if (enumMemberMappings.TryGetValue(constant, out string? engineMemberName))
                {
                    context.TypeDB.RegisterMemberMapping(enumType, engineMemberName, constant);

                    // Also register it as a global member so it can resolved from the documentation
                    // when the enum name is omitted.
                    context.TypeDB.RegisterGlobalMemberMapping($"@GlobalScope.{engineMemberName}", $"{enumType.FullNameWithGlobal}.{constant.Name}");
                }
            }
        }
    }
}
