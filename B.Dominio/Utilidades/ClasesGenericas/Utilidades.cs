using AutoMapper;
using Microsoft.Extensions.Configuration;
using Modelo.ClasesGenericas;
using Modelo.Validaciones;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Utilidades.Interfaces;
using static Modelo.ClasesGenericas.AppConfig;

namespace Utilidades.ClasesGenericas
{
    public class Utilidad: IUtilidades
    {
        private  AppConfig config;
        internal IConfiguration _config { get; set; }

        public Utilidad(IConfiguration config)
        {
            _config = config;
            this.config = _config.Get<AppConfig>();
        }

        public Utilidad()
        {
            try
            {
                string Dominio = string.Empty;
                try
                {
                    Dominio = Directory.GetCurrentDirectory();                   
                }
                catch
                {
                    Dominio = AppDomain.CurrentDomain.BaseDirectory;
                }

                var builder = new ConfigurationBuilder()
                            .SetBasePath(Dominio)
                            .AddJsonFile("appsettings.json", false, true);
                _config = builder.Build();
                config  = _config.Get<AppConfig>();
            }
            catch { }
        }
        public DateTime DateFormat(string fecha)
        {
            return Util.IsDateValid(fecha);
        }

        public string Right(string value, int length)
        {
            return Util.Right(value, length);
        }
        public AppConfig Options { set { config = value; } get { return config; } }
        public string CambiarLetras(string str)
        {
            return Regex.Replace(str, @"[^0-9A-Za-z ]", "", RegexOptions.None);
        }
        public string AnyoMesLetras(DateTime fecha)
        {
            return Util.AnyoMesLetras(fecha);
        }
        public DataTable Excel_To_DataTable(MemoryStream fs, string Name, string sFileExtension, int numSheet)
        {
            return DataTableHelper.CreateTable(fs, Name, sFileExtension, numSheet);
        }
        public string Encrypt(string password)
        {
            byte[] keybytes = Encoding.UTF8.GetBytes(GetKey);
            byte[] ivbytes = Encoding.UTF8.GetBytes(GetIV);

            if (keybytes.Length > 0 && keybytes.Length < 16)
                Array.Resize(ref keybytes, 16);
            if ((keybytes.Length > 16 && keybytes.Length < 32) || keybytes.Length > 32)
                Array.Resize(ref keybytes, 32);

            if (ivbytes.Length != 16)
                Array.Resize(ref ivbytes, 16);

            byte[] encrypted;
            using (var rijAlg = new RijndaelManaged())
            {
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.ISO10126;
                rijAlg.FeedbackSize = 128;
                rijAlg.Key = keybytes;
                rijAlg.IV = ivbytes;

                var encryptor = rijAlg.CreateEncryptor(rijAlg.Key, rijAlg.IV);
                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(password);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            return Convert.ToBase64String(encrypted);
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);
            byte[] keybytes = Encoding.UTF8.GetBytes(GetKey);
            byte[] ivbytes = Encoding.UTF8.GetBytes(GetIV);

            if (keybytes.Length > 0 && keybytes.Length < 16)
                Array.Resize(ref keybytes, 16);
            if ((keybytes.Length > 16 && keybytes.Length < 32) || keybytes.Length > 32)
                Array.Resize(ref keybytes, 32);

            if (ivbytes.Length != 16)
                Array.Resize(ref ivbytes, 16);

            string plaintext = null;
            using (var rijAlg = new RijndaelManaged())
            {
                rijAlg.Mode = CipherMode.CBC;
                rijAlg.Padding = PaddingMode.ISO10126;
                rijAlg.FeedbackSize = 128;
                rijAlg.Key = keybytes;
                rijAlg.IV = ivbytes;

                var decryptor = rijAlg.CreateDecryptor(rijAlg.Key, rijAlg.IV);
                try
                {
                    using (var msDecrypt = new MemoryStream(cipherTextBytes))
                    {
                        using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (var srDecrypt = new StreamReader(csDecrypt))
                            {
                                plaintext = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
                catch
                {
                    plaintext = "Key o IV Incorrecto";
                }
            }

            return plaintext;
        }

        private string GetKey
        {
            get { return config.AppSettingsKey.key ?? ""; }
        }
        private string GetIV
        {
            get { return config.AppSettingsKey.iv ?? ""; }
        }

        public void MapTUpDList(object Out, object parameters)
        {           
            if (parameters != null)
            {
                foreach (PropertyInfo propertyInfo in parameters.GetType().GetProperties())
                {
                    string nameIn = propertyInfo.Name;
                    object valueIn = propertyInfo.GetValue(parameters, null);
                    foreach (PropertyInfo propertyInfoO in Out.GetType().GetProperties())
                    {
                        string nameInO = propertyInfoO.Name;
                        object valueInO = propertyInfoO.GetValue(Out, null);
                        if (nameIn == nameInO)
                            propertyInfoO.SetValue(Out, valueIn, null); 
                    }
                }
            }
        }
        public T XmlToObjet<T>(string testData)
        {
            return Util.XmlToObjet<T>(testData);
        }
        public List<T> ObtenerDato<T>(string dato)
        {
            var resp = JsonConvert.DeserializeObject<List<T>>(dato);
            return resp;
        }

        public T ObtenerRegistro<T>(string dato)
        {
            var resp = JsonConvert.DeserializeObject<T>(dato);
            return resp;
        }

        public string FormaDatos<T>(List<T> dato)
        {
            string respusta = JsonConvert.SerializeObject(dato);
            return respusta.ToString();
        }

        public TOut MapDatos<TOut, TIn>(TIn fromRecord) where TOut : new()
        {
            return  MappingService.Map<TOut, TIn>(fromRecord);
        }

        public HttpContent CreateHttpContent<T>(T content)
        {
            var json = JsonConvert.SerializeObject(content, MicrosoftDateFormatSettings);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        private static JsonSerializerSettings MicrosoftDateFormatSettings
        {
            get
            {
                return new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
                };
            }
        }

        public List<InfoContex> ConnectionStrings
        {
            get {
                
                    return config.ConnectionStrings.Contexto; 
               }
        }

        public List<InfoiSoap> SoapConfig
        {
            get { return config.ConfigSoap.Soap; }
        }
        public AppSettingsSW ConfigService
        {
            get { return config.ConfigService; }
        }
        public AppConfig.AppReportInfo Inforeport
        {
            get { return config.ReportInfo; }
        }

        public string BaseUrlApiLog
        {
            get { return config.ApiLogInfo.baseUrl; }
        }
        public string BaseUrlRedmine
        {
            get { return config.ApiLogInfo.baseUrlRedmine; }
        }

        public string ApiKeyRedmine
        {
            get { return config.ApiLogInfo.apiKeyRedmine; }
        }
        public int SistemaId
        {
            get { return int.Parse(config.AppSettingsKey.sis); }
        }

        public DataTable ConvertTo<T>(List<T> list, string nombre)
        {
            return DataTableHelper.ConvertTo<T>(list, nombre);
        }

        public List<T> ConvertTo<T>(DataTable dt)
        {
            return DataTableHelper.ConvertTo<T>(dt);
        }
        public IQueryable<T> MapToListReader<T>(DbDataReader dr)
        {
            var objList = new List<T>();
            var props = typeof(T).GetRuntimeProperties();

            var colMapping = dr.GetColumnSchema()
              .Where(x => props.Any(y => y.Name.ToLower() == x.ColumnName.ToLower()))
              .ToDictionary(key => key.ColumnName.ToLower());

            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    T obj = Activator.CreateInstance<T>();
                    foreach (var prop in props)
                    {
                        var val = dr.GetValue(colMapping[prop.Name.ToLower()].ColumnOrdinal.Value);
                        prop.SetValue(obj, val == DBNull.Value ? null : val);
                    }
                    objList.Add(obj);
                }
            }
            return objList.AsQueryable();
        }
        /// <summary>
        /// Convertir una cadena de texto xml a xml node
        /// </summary>
        /// <param name="pCadenaXml">cadena xml</param>
        /// <returns>Ojeto XmlNode</returns>
        public XmlNode ToXmlNode(string pCadenaXml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(pCadenaXml);
            XmlNode newNode = doc.DocumentElement;
            return newNode;
        }
        public XmlDocument ToXmldoc<T>(T tipo)
        {
            return Util.ToXmldoc(tipo);
        }
        public string ToXml<T>(T tipo)
        {
            return Util.ToXml(tipo).Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>", "").Replace(" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"", ""); ;
        }
    }

    public static class MappingService
    {
        /// <summary>
        /// Converts model from class F to class T
        /// </summary>
        /// <typeparam name="T">To Class</typeparam>
        /// <typeparam name="F">From Class</typeparam>
        /// <returns>model of type class T</returns>
  
        public static TOut Map<TOut, TIn>(TIn from) where TOut : new()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(from);
            var to   = System.Text.Json.JsonSerializer.Deserialize<TOut>(json);

            return to;
        }
    }
    public class AutoMapperGenericsHelper : Profile
    {
        public TDestination ConvertToDBEntity<TDestination, TSource>(TSource model)
        {
            /*var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AllowNullCollections = true;
                cfg.ForAllMaps((map, exp) =>
                {
                    foreach (var unmappedPropertyName in map.GetUnmappedPropertyNames())
                        exp.ForMember(unmappedPropertyName, opt => opt.Ignore());
                });

                // Register Model & Dto will be map here.
                cfg.CreateMap<TSource, TDestination>();

            });*/
            var mapperConfig = new MapperConfiguration(cfg => cfg.CreateMap(typeof(TSource), typeof(TDestination)));
            var mapper = new Mapper(mapperConfig);
            var destino = mapper.DefaultContext.Mapper.Map<TSource, TDestination>(model);
            return destino;
        }
    }    
}
