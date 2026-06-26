using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace Modelo.ClasesGenericas
{
	/// <summary>
	/// Manejo de respuesta de error y parametros de salida de rutinas DB2
	/// </summary>
	public class ErrorDB2
	{
		[XmlElement("CodError")]
		public int CodError { set; get; }
		[XmlElement("MensajeError")]
		public string OVARMENSAJEERROR { set; get; }

		/// <summary>
		/// Definir los parametros de salida con objeto anómino 
		/// new {NOMPARAMETRO = valor}
		/// </summary>
		public object OUTParam { set; get; }
		/// <summary>
		/// Diccionario con los parametros de salida de la rutina
		/// </summary>
		public Dictionary<string, string> DicOutParms { get; set; }

		public ErrorDB2()
		{
			DicOutParms = new Dictionary<string, string>();
			CodError = 0;
			OVARMENSAJEERROR = "";
		}
		/// <summary>
		/// Responder los parametros de salida hacia una clase tipada
		/// </summary>
		/// <typeparam name="Tmodel"></typeparam>
		/// <returns></returns>
		public Tmodel ParamOutValue<Tmodel>() where Tmodel : class, new()
		{
			var model = new Tmodel();
			MapTUpDList(model);
			return model;
		}

		public object AddDEfaultParam
		{
			get { return new { CodErrorR = CodError, OVARMENSAJEERROR = OVARMENSAJEERROR }; }
		}

		public void DefaultParamOut()
		{
			OUTParam = new { CodErrorR = CodError, OVARMENSAJEERROR = OVARMENSAJEERROR };
		}

		private void MapTUpDList(object Out)
		{
			foreach (PropertyInfo prop in Out.GetType().GetProperties())
			{
				string nameInO = prop.Name;
				var item = DicOutParms.Where(x => x.Key == nameInO).FirstOrDefault();
				if (item.Key != null && item.Key.Equals(nameInO))
				{
					TypeCode typeCode = Type.GetTypeCode(prop.PropertyType);
					switch (typeCode)
					{
						case TypeCode.Int32:
							prop.SetValue(Out, int.Parse(item.Value), null);
							break;
						case TypeCode.Int64:
							prop.SetValue(Out, Int64.Parse(item.Value), null);
							break;
						case TypeCode.Decimal:
							prop.SetValue(Out, double.Parse(item.Value), null);
							break;
						default:
							prop.SetValue(Out, item.Value, null);
							break;
					}
				}
			}
		}
	}
}
