﻿using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;

namespace UtilsSharp
{
    /// <summary>
    /// Appsettings帮助类
    /// </summary>
    public static class AppsettingsHelper
    {
        private static IConfiguration config;
        static AppsettingsHelper()
        {
            var builder = new ConfigurationBuilder();//创建config的builder
            builder.SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");//设置配置文件所在的路径加载配置文件信息
            config = builder.Build();
        }

        /// <summary>
        /// 根据key获取对应的配置值
        /// </summary>
        /// <param name="key">key</param>
        /// <returns></returns>
        public static string GetValue(string key)
        {
            return config[key];
        }

        /// <summary>
        /// 获取ConnectionStrings下默认的配置连接字符串
        /// </summary>
        /// <param name="key">key</param>
        /// <returns></returns>
        public static string GetConnectionString(string key)
        {
            return config.GetConnectionString(key);
        } 

        /// <summary>
        /// 获取相应节点
        /// </summary>
        /// <param name="key">key</param>
        /// <returns></returns>
        public static IConfigurationSection GetSection(string key)
        {
            return config.GetSection(key);
        }

        /// <summary>
        /// 获取所有节点
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<IConfigurationSection> GetChildren()
        {
            return config.GetChildren();
        }
    }
}
