using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewMaterializerCache.Attributes
{
	public class PropertyLookupDtoAttribute : Attribute
	{
		/// <summary>
		/// Type of what DTO the value will be fetched from
		/// </summary>
		public Type SourceDto { get; set; }

		/// <summary>
		/// What property on the SourceDto will be used to get the value for this property.
		/// Common usage, nameof(SourceDto.PropertyName)
		/// </summary>
		public string DtoPropertyName { get; set; }

		public PropertyLookupDtoAttribute(Type SourceDto, string DtoPropertyName)
		{
			this.SourceDto = SourceDto;
			this.DtoPropertyName = DtoPropertyName;
		}
	}
}
