﻿using System;
using System.Collections.Generic;
using System.Xml.XPath;
using System.Reflection;
using System.IO;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Xunit;

namespace Swashbuckle.AspNetCore.SwaggerGen.Test
{
    public class XmlCommentsSchemaFilterTests
    {
        private readonly IApiModelResolver _apiModelResolver;

        public XmlCommentsSchemaFilterTests()
        {
            _apiModelResolver = new JsonApiModelResolver(new JsonSerializerOptions());
        }

        [Theory]
        [InlineData(typeof(XmlAnnotatedType), "Summary for XmlAnnotatedType")]
        [InlineData(typeof(XmlAnnotatedType.NestedType), "Summary for NestedType")]
        [InlineData(typeof(XmlAnnotatedGenericType<int, string>), "Summary for XmlAnnotatedGenericType")]
        public void Apply_SetsDescription_FromSummaryTag(
            Type type,
            string expectedDescription)
        {
            var schema = new OpenApiSchema
            {
                Properties = new Dictionary<string, OpenApiSchema>()
            };
            var filterContext = FilterContextFor(type);

            Subject().Apply(schema, filterContext);

            Assert.Equal(expectedDescription, schema.Description);
        }

        [Theory]
        [InlineData(typeof(XmlAnnotatedType), nameof(XmlAnnotatedType.StringProperty), "Summary for StringProperty")]
        [InlineData(typeof(XmlAnnotatedSubType), nameof(XmlAnnotatedType.StringProperty), "Summary for StringProperty")]
        [InlineData(typeof(XmlAnnotatedGenericType<string, bool>), "GenericProperty", "Summary for GenericProperty")]
        public void Apply_SetsPropertyDescriptions_FromPropertySummaryTags(
            Type type,
            string propertyName,
            string expectedDescription)
        {
            var schema = new OpenApiSchema
            {
                Properties = new Dictionary<string, OpenApiSchema>()
                {
                    { propertyName, new OpenApiSchema() }
                }
            };
            var filterContext = FilterContextFor(type);

            Subject().Apply(schema, filterContext);

            Assert.Equal(expectedDescription, schema.Properties[propertyName].Description);
        }

        [Theory]
        [InlineData(typeof(XmlAnnotatedType), nameof(XmlAnnotatedType.BoolProperty), "boolean", null, true)]
        [InlineData(typeof(XmlAnnotatedType), nameof(XmlAnnotatedType.IntProperty), "integer", "int32", 10)]
        [InlineData(typeof(XmlAnnotatedType), nameof(XmlAnnotatedType.LongProperty), "integer", "int64", 4294967295L)]
        [InlineData(typeof(XmlAnnotatedType), nameof(XmlAnnotatedType.FloatProperty), "number", "float", 1.2F)]
        [InlineData(typeof(XmlAnnotatedType), nameof(XmlAnnotatedType.DoubleProperty), "number", "double", 1.25D)]
        [InlineData(typeof(XmlAnnotatedType), nameof(XmlAnnotatedType.EnumProperty), "integer", "int32", 2)]
        [InlineData(typeof(XmlAnnotatedType), nameof(XmlAnnotatedType.GuidProperty), "string", "uuid", "d3966535-2637-48fa-b911-e3c27405ee09")]
        [InlineData(typeof(XmlAnnotatedType), nameof(XmlAnnotatedType.StringProperty), "string", null, "Example for StringProperty")]
        [InlineData(typeof(XmlAnnotatedType), nameof(XmlAnnotatedType.BadExampleIntProperty), "integer", "int32", null)]
        public void Apply_SetsPropertyExample_FromPropertyExampleTags(
            Type memberType,
            string memberName,
            string schemaType,
            string schemaFormat,
            object expectedValue)
        {
            var schema = new OpenApiSchema
            {
                Properties = new Dictionary<string, OpenApiSchema>()
                {
                    { memberName, new OpenApiSchema { Type = schemaType, Format = schemaFormat } }
                }
            };
            var filterContext = FilterContextFor(memberType);

            Subject().Apply(schema, filterContext);

            var openApiPrimitive = (IOpenApiPrimitive)schema.Properties[memberName].Example;

            if (expectedValue != null)
                Assert.Equal(expectedValue, openApiPrimitive.GetType().GetProperty("Value").GetValue(openApiPrimitive));
            else
                Assert.Null(openApiPrimitive);
        }

        [Theory]
        [InlineData(typeof(XmlAnnotatedType), "MissingStringProperty", "string")]
        [InlineData(typeof(XmlAnnotatedType), "MissingIntegerProperty", "integer")]
        public void Apply_IgnoresNonexistingProperty(Type type,
            string propertyName,
            string propertyType)
        {
            var schema = new OpenApiSchema
            {
                Properties = new Dictionary<string, OpenApiSchema>()
                {
                    { propertyName, new OpenApiSchema() { Type = propertyType } }
                }
            };
            var filterContext = FilterContextFor(type);

            Subject().Apply(schema, filterContext);

            var openApiSchema = schema.Properties[propertyName];
            Assert.Equal(propertyType, openApiSchema.Type);
        }

        private SchemaFilterContext FilterContextFor(Type type)
        {
            return new SchemaFilterContext(
                _apiModelResolver.ResolveApiModelFor(type),
                schemaRepository: null, // NA for test
                schemaGenerator: null // NA for test
            );
        }

        private XmlCommentsSchemaFilter Subject()
        {
            using (var xmlComments = File.OpenText(GetType().GetTypeInfo()
                    .Assembly.GetName().Name + ".xml"))
            {
                return new XmlCommentsSchemaFilter(new XPathDocument(xmlComments));
            }
        }
    }
}