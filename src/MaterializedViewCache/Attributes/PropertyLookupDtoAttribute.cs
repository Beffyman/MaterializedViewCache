using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterializedViewCache.Attributes
{
	/// <summary>
	/// Attribute that defines what dto property this property will be mapped from
	/// </summary>
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


		/// <summary>
		/// Default Constructor
		/// </summary>
		/// <param name="SourceDto"></param>
		/// <param name="DtoPropertyName"></param>
		public PropertyLookupDtoAttribute(Type SourceDto, string DtoPropertyName)
		{
			this.SourceDto = SourceDto;
			this.DtoPropertyName = DtoPropertyName;
		}
	}
}
