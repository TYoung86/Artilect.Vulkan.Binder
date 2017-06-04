﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using Artilect.Vulkan.Binder.Extensions;

namespace Artilect.Vulkan.Binder {
	public partial class InteropAssemblyBuilder {
		private Func<Type[]> DefineClrType(ClangUnionInfo unionInfo) {
			if (unionInfo.Size == 0) {
				throw new NotImplementedException();
			}
			TypeBuilder unionDef = Module.DefineType(unionInfo.Name,
				PublicSealedUnionTypeAttributes, null,
				(PackingSize) unionInfo.Alignment,
				(int) unionInfo.Size);
			unionDef.SetCustomAttribute(StructLayoutExplicitAttributeInfo);
			var fieldParams = new LinkedList<CustomParameterInfo>(unionInfo.Fields.Select(f => ResolveField(f.Type, f.Name, (int) f.Offset)));

			return () => {
				foreach (var fieldParam in fieldParams)
					fieldParam.RequireCompleteTypes(true);

				fieldParams.ConsumeLinkedList(fieldParam => {
					var fieldName = fieldParam.Name;
					var fieldType = fieldParam.ParameterType;
					if (fieldType is IncompleteType)
						throw new InvalidProgramException("Encountered incomplete type in structure field definition.");
					var fieldDef = unionDef.DefineField(fieldName, fieldType, FieldAttributes.Public);
					fieldDef.SetCustomAttribute(AttributeInfo.Create(
						() => new FieldOffsetAttribute(fieldParam.GetPosition())));
					foreach (var attr in fieldParam.AttributeInfos)
						fieldDef.SetCustomAttribute(attr);
				});

				return new[] {unionDef.CreateType()};
			};
		}
	}
}