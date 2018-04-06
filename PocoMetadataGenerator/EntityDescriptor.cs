﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Breeze.PocoMetadata
{
    /// <summary>
    /// Describes the metadata for a set of entities.
    /// 
    /// The PocoMetadataBuilder calls methods on this class to determine how to generate
    /// metadata for the entities.
    /// 
    /// Extend this class to adapt to your data model.
    /// </summary>
    public class EntityDescriptor
    {
        /// <summary>
        /// Filter types from metadata generation.
        /// </summary>
        /// <returns>
        /// true if a Type should be included, false otherwise.
        /// </returns>
        /// <example>
        /// // exclude certain entities, and all Audit* entities
        /// var excluded = new string[] { "Comment", "LogRecord", "UserPermission" };
        /// bool Include(Type type)
        /// {
        ///   if (excluded.Contains(type.Name)) return false;
        ///   if (type.Name.StartsWith("Audit")) return false;
        ///   return true;
        /// };
        /// </example>
        //WHAT
        public virtual bool Include(Type type)
        {
            return true;
        }

        /// <summary>
        /// Replace the given type wherever it appears in the metadata.
        /// Can be used to replace an interface with a class.
        /// </summary>
        /// <param name="type">Type to replace</param>
        /// <param name="types">List of available types provided to the PocoMetadataBuilder</param>
        /// <returns>Replacement type</returns>
        //WHAT
        public virtual Type Replace(Type type, IEnumerable<Type> types)
        {
            return type;
        }

        /// <summary>
        /// Get the autoGeneratedKeyType value for the given type.  Should be defined even if the actual key property is on a base type.
        /// </summary>
        /// <param name="type">Entity type for which metadata is being generated</param>
        /// <returns>One of:
        /// "Identity" - key is generated by db server, or is a Guid.
        /// "KeyGenerator" - key is generated by code on app server, e.g. using Breeze.ContextProvider.IKeyGenerator 
        /// "None" - key is not auto-generated, but is assigned manually.
        /// null - same as None.
        /// </returns>
        //WHAT
        public virtual string GetAutoGeneratedKeyType(Type type)
        {
            return null;
        }

        /// <summary>
        /// Get the server resource name (endpoint) for the given type.  E.g. for entity type Product, it might be "Products".
        /// This value is used by Breeze client when composing a query URL for an entity.
        /// </summary>
        /// <param name="type">Entity type for which metadata is being generated</param>
        /// <returns>Resource name</returns>
        public virtual string GetResourceName(Type type)
        {
            return Pluralize(type.Name);
        }

        /// <summary>
        /// Determine if the given type is a "Complex Type" instead of an "Entity".
        /// Complex Types are sometimes called component types or embedded types, because they are 
        /// part of the parent entity instead of being related by foreign keys.
        /// </summary>
        /// <param name="type">Entity type for which metadata is being generated</param>
        /// <returns>true for a complex type, false for an entity</returns>
        //WHAT
        public virtual bool IsComplexType(Type type)
        {
            return false;
        }

        /// <summary>
        /// Determine if the property is part of the entity key.
        /// </summary>
        /// <param name="type">Entity type for which metadata is being generated</param>
        /// <param name="propertyInfo">Property being considered</param>
        /// <returns>True if property is part of the entity key, false otherwise</returns>
        public virtual bool IsKeyProperty(Type type, PropertyInfo propertyInfo)
        {
            var name = propertyInfo.Name;
            if (name == (type.Name + "ID")) return true;
            if (name == "ID") return true;
            return false;
        }

        /// <summary>
        /// Determine if the property is a version property used for optimistic concurrency control.
        /// </summary>
        /// <param name="type">Entity type for which metadata is being generated</param>
        /// <param name="propertyInfo">Property being considered</param>
        /// <returns>True if property is the entity's version property, false otherwise</returns>
        public virtual bool IsVersionProperty(Type type, PropertyInfo propertyInfo)
        {
            if (propertyInfo.Name == "RowVersion") return true;
            return false;
        }

        /// <summary>
        /// Change the type of the given data property in the metadata.
        /// For example, a custom wrapper type on the server may be unwrapped on the client, and the metadata reflects this.
        /// </summary>
        /// <param name="type">Entity type for which metadata is being generated</param>
        /// <param name="propertyInfo">Property being considered</param>
        /// <returns>Type of the property to put in the metadata, or null to exclude the property.</returns>
        public virtual Type GetDataPropertyType(Type type, PropertyInfo propertyInfo)
        {
            return propertyInfo.PropertyType;
        }

        /// <summary>
        /// Get the foreign key for the given scalar navigation property.  This is another property on the 
        /// same entity that establishes the foreign key relationship.
        /// </summary>
        /// <param name="type">Entity type for which metadata is being generated</param>
        /// <param name="propertyInfo">Scalar navigation/association property</param>
        /// <returns>Name of the related property</returns>
        /// <example>if Order has a Customer navigation property, that is related via the CustomerID data property, 
        /// we would return "CustomerID"
        /// </example>
        public virtual string GetForeignKeyName(Type type, PropertyInfo propertyInfo)
        {
            return propertyInfo.Name + "ID";
        }

        /// <summary>
        /// Return the name of the inverse relationship property for a navigation property.  
        /// This is a property on the related type that points back to the containing type.
        /// </summary>
        /// <param name="containingType">Type containing the propertyInfo</param>
        /// <param name="propertyName">Navigation property pointing to relatedType</param>
        /// <param name="relatedType">Type related to containingType by propertyInfo</param>
        /// <returns>Name of property on relatedType that points back to containingType, 
        ///     or empty string to indicate that there is no inverse property,
        ///     or null to let the inverse be inferred automatically</returns>
        public virtual string GetInversePropertyName(Type containingType, string propertyName, Type relatedType)
        {
            return null;
        }

        /// <summary>
        /// Determine the generator behaves if a foreign key data property cannot be found for the given navigation property.
        /// </summary>
        /// <param name="type">Entity type for which metadata is being generated</param>
        /// <param name="propertyInfo">Scalar navigation/association property</param>
        /// <returns>MissingKeyHandling.Error; override to change this behavior</returns>
        public virtual MissingKeyHandling GetMissingFKHandling(Type type, PropertyInfo propertyInfo)
        {
            return MissingKeyHandling.Error;
        }

        /// <summary>
        /// Determine the generator behaves if a primary key is missing on an entity.
        /// </summary>
        /// <param name="type">Entity type for which metadata is being generated</param>
        /// <returns>MissingKeyHandling.Error; override to change this behavior</returns>
        public virtual MissingKeyHandling GetMissingPKHandling(Type type)
        {
            return MissingKeyHandling.Error;
        }


        /// <summary>
        /// Lame pluralizer.  Assumes we just need to add a suffix.  
        /// Consider using System.Data.Entity.Design.PluralizationServices.PluralizationService.
        /// </summary>
        /// <param name="s">String to pluralize</param>
        /// <returns>Pseudo-pluralized string</returns>
        public virtual string Pluralize(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var last = s.Length - 1;
            var c = s[last];
            switch (c)
            {
                case 'y':
                    return s.Substring(0, last) + "ies";
                default:
                    return s + 's';
            }
        }

        /// <summary>
        /// Change first letter to lowercase
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        protected virtual string camelCase(string s)
        {
            if (string.IsNullOrEmpty(s) || !char.IsUpper(s[0]))
            {
                return s;
            }
            string str = char.ToLower(s[0]).ToString();
            if (s.Length > 1)
            {
                str = str + s.Substring(1);
            }
            return str;

        }

        /// <summary>
        /// Maps a DataAnnotations validation attribute to the corresponding client validation descriptor
        /// </summary>
        /// <param name="attr">The validation attribute</param>
        /// <param name="definition">The definition of the corresponding entity/property. Additional definitions can be added or removed.</param>
        /// <returns>Validator descriptor</returns>
        public virtual Dictionary<string, object>[] MapValidationAttribute(ValidationAttribute attr, Dictionary<string, object> definition)
        {
            return null;
        }

        /// <summary>
        /// Allows for final processing of the list of validators.
        /// </summary>
        /// <param name="validators">The proposed list of validators/param>
        /// <param name="definition">The definition of the corresponding entity/property. Additional definitions can be added or removed.</param>
        /// <returns>Final list of validators</returns>
        public virtual IEnumerable<Dictionary<string, object>> PostProcessValidators(IEnumerable<Dictionary<string, object>> validators, Dictionary<string, object> definition)
        {
            return validators;
        }
    }

    /// <summary>
    /// How to handle a missing foreign key for a navigation property
    /// </summary>
    public enum MissingKeyHandling
    {
        /// <summary>Throw an error and stop the metadata generation</summary>
        Error,
        /// <summary>Write the error to stderr and continue</summary>
        Log,
        /// <summary>Add a new foreign key using the expected name</summary>
        Add
    }
}
