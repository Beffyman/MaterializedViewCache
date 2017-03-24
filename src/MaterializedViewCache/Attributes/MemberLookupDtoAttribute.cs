using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaterializedViewCache.Attributes
{
	/// <summary>
	/// Attribute that defines what dto member this member will be mapped from
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple =false,Inherited =false)]
	public class MemberLookupDtoAttribute : Attribute
	{
		/// <summary>
		/// Type of what DTO the value will be fetched from
		/// </summary>
		public Type SourceDto { get; set; }

		/// <summary>
		/// What member on the SourceDto will be used to get the value for this member.
		/// Common usage, nameof(SourceDto.PropertyName)
		/// </summary>
		public string DtoMemberName { get; set; }


		/// <summary>
		/// Default Constructor
		/// </summary>
		/// <param name="SourceDto"></param>
		/// <param name="DtoMemberName"></param>
		public MemberLookupDtoAttribute(Type SourceDto, string DtoMemberName)
		{
			this.SourceDto = SourceDto;
			this.DtoMemberName = DtoMemberName;
		}
	}
}
