using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Vidyano.Service.Metadata;

namespace Vidyano.Service.EntityFrameworkCore.Metadata
{
    public class EntityFrameworkEntityProperty : EntityProperty
    {
        /// <inheritdoc />
        public EntityFrameworkEntityProperty(IProperty property)
            : base(property.Name, property.ClrType)
        {
            MaxLength = property.GetMaxLength();
            IsNullable = property.IsNullable;
            IsReadOnly = property.ValueGenerated != ValueGenerated.Never;
        }
    }
}