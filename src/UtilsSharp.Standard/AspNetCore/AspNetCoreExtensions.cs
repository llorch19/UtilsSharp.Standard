﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using AspNetCore.Swagger;
using Microsoft.Extensions.DependencyInjection;
using UtilsSharp;

namespace AspNetCore
{
    /// <summary>
    /// AspNetCoreExtensions
    /// </summary>
    public static class AspNetCoreExtensions
    {
        /// <summary>
        /// 添加AspNetCore扩展服务
        /// </summary>
        /// <param name="services">services</param>
        /// <param name="swaggerDocOptions">swaggerDocOptions</param>
        /// <returns></returns>
        public static IServiceCollection AddAspNetCoreExtensions(this IServiceCollection services, SwaggerDocOptions swaggerDocOptions=null)
        {
            services.AddSwaggerExtensions(swaggerDocOptions);
            return services;
        }

        /// <summary>
        /// 添加Swagger扩展服务
        /// </summary>
        /// <param name="services">services</param>
        /// <param name="swaggerDocOptions">swaggerDocOptions</param>
        /// <returns></returns>
        public static IServiceCollection AddSwaggerExtensions(this IServiceCollection services, SwaggerDocOptions swaggerDocOptions = null)
        {
            if (swaggerDocOptions == null)
            {
                swaggerDocOptions = new SwaggerDocOptions();
            }
            AspNetCoreExtensionsConfig.SwaggerDocOptions = swaggerDocOptions;
            if (swaggerDocOptions.Enable)
            {
                services.AddSwaggerGen(c =>
                {
                    c.CustomSchemaIds(i => i.FullName);
                    c.SwaggerDoc(swaggerDocOptions.Name, swaggerDocOptions.OpenApiInfo);
                    var enumerable = AssemblyHelper.GetAllAssemblies();
                    foreach (var item in enumerable)
                    {
                        var xmlName = $"{item.GetName().Name}.xml";
                        var xmlPath = item.ManifestModule.FullyQualifiedName.Replace(item.ManifestModule.Name, xmlName);
                        if (File.Exists(xmlPath))
                        {
                            c.IncludeXmlComments(xmlPath, true); //添加控制器层注释（true表示显示控制器注释）
                        }
                    }
                    if (swaggerDocOptions.HeaderParameters != null && swaggerDocOptions.HeaderParameters.Count > 0)
                    {
                        c.OperationFilter<AddRequiredHeaderParameter>();//添加header参数
                    }
                });
            }
            return services;
        }

    }
}
