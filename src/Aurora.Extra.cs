//
// Aurora.Extra - Additional bits that may be useful in your applications      
//
// Updated On: 30 March 2014
//
// Contact Info:
//
//  Frank Hale - <frankhale@gmail.com> 
//
// LICENSE: Unless otherwise stated all code is under the GNU GPLv3
// 
// GPL version 3 <http://www.gnu.org/licenses/gpl-3.0.html> (see below)
//
// NON-GPL code = my fork of Rob Conery's Massive which is under the 
//                "New BSD License"
//

#region LICENSE - GPL version 3 <http://www.gnu.org/licenses/gpl-3.0.html>
//
// NOTE: Aurora contains some code that is not licensed under the GPLv3. 
//       that code has been labeled with it's respective license below. 
//
// NON-GPL code = Rob Conery's Massive which is under the "New BSD License" and
//                My Gravatar fork which the original author did not include
//                a license.
//
// GNU GPLv3 quick guide: http://www.gnu.org/licenses/quick-guide-gplv3.html
//
// GNU GPLv3 license <http://www.gnu.org/licenses/gpl-3.0.html>
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
#endregion

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Web;

#if MASSIVE
using System.Data;
using System.Data.Common;
using System.Dynamic;
#endif

#if ACTIVEDIRECTORY
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
#endif

#if OPENAUTH
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.Extensions.SimpleRegistration;
using DotNetOpenAuth.OpenId.RelyingParty;
#endif

namespace Aurora.Extra
{
	#region ATTRIBUTES
	[AttributeUsage(AttributeTargets.All)]
	public sealed class MetadataAttribute : Attribute
	{
		public string Metadata { get; private set; }

		public MetadataAttribute(string metadata)
		{
			Metadata = metadata;
		}
	}

	public enum DescriptiveNameOperation
	{
		SplitCamelCase,
		None
	}

	[AttributeUsage(AttributeTargets.All)]
	public sealed class DescriptiveNameAttribute : Attribute
	{
		public string Name { get; private set; }

		public DescriptiveNameOperation Op { get; private set; }

		public DescriptiveNameAttribute(string name)
		{
			Name = name;
			Op = DescriptiveNameOperation.None;
		}

		public DescriptiveNameAttribute(DescriptiveNameOperation op)
		{
			Name = string.Empty; // Name comes from property name

			Op = op; // We'll perform an operation on the property name like put spacing between camel case names, then title case the name.
		}

		public string PerformOperation(string name)
		{
			// This regex comes from this StackOverflow question answer:
			// http://stackoverflow.com/questions/155303/net-how-can-you-split-a-caps-delimited-string-into-an-array
			return Op == DescriptiveNameOperation.SplitCamelCase ? Regex.Replace(name, @"([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ") : null;
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public sealed class DateFormatAttribute : Attribute
	{
		public string Format { get; set; }

		public DateFormatAttribute(string format)
		{
			Format = format;
		}
	}

	[AttributeUsage(AttributeTargets.Property)]
	public sealed class StringFormatAttribute : Attribute
	{
		public string Format { get; set; }

		public StringFormatAttribute(string format)
		{
			Format = format;
		}
	}
	#endregion

	#region EXTENSION METHODS
	public static class ExtensionMethods
	{
		/// <summary>
		/// Converts a lowercase string to title case
		/// <remarks>
		/// Adapted from: http://stackoverflow.com/questions/271398/what-are-your-favorite-extension-methods-for-c-codeplex-com-extensionoverflow
		/// </remarks>
		/// </summary>
		/// <param name="value">String to convert</param>
		/// <returns>A title cased string</returns>
		public static string ToTitleCase(this string value)
		{
			if (string.IsNullOrEmpty(value))
			{
				return value;
			}

			var cultureInfo = System.Threading.Thread.CurrentThread.CurrentCulture;
			var textInfo = cultureInfo.TextInfo;

			// TextInfo.ToTitleCase only operates on the string if is all lower case, otherwise it returns the string unchanged.
			return textInfo.ToTitleCase(value.ToLower());
		}

		/// <summary>
		/// Takes a camel cased string and returns the string with spaces between the words
		/// <remarks>
		/// from http://stackoverflow.com/questions/271398/what-are-your-favorite-extension-methods-for-c-codeplex-com-extensionoverflow
		/// </remarks>
		/// </summary>
		/// <param name="camelCaseWord">The input string</param>
		/// <returns>A string with spaces between words</returns>
		public static string Wordify(this string camelCaseWord)
		{
			// if the word is all upper, just return it
			return !Regex.IsMatch(camelCaseWord, "[a-z]") ? camelCaseWord : string.Join(" ", Regex.Split(camelCaseWord, @"(?<!^)(?=[A-Z])"));
		}

		public static string GetMetadata(this Enum obj)
		{
			if (obj != null)
			{
				var mda = (MetadataAttribute)obj.GetType().GetField(obj.ToString()).GetCustomAttributes(false).FirstOrDefault(x => x is MetadataAttribute);

				if (mda != null)
					return mda.Metadata;
			}

			return null;
		}

		public static string GetDescriptiveName(this Enum obj)
		{
			if (obj != null)
			{
				var dna = (DescriptiveNameAttribute)obj.GetType().GetField(obj.ToString()).GetCustomAttributes(false).FirstOrDefault(x => x is DescriptiveNameAttribute);

				if (dna != null)
					return dna.Name;
			}

			return null;
		}
	}
	#endregion

	#region ENCRYPTION
	public static class Encryption
	{
		private static byte[] GetPassphraseHash(string passphrase, int size)
		{
			byte[] phash;

			using (var hashsha1 = new SHA1CryptoServiceProvider())
			{
				phash = hashsha1.ComputeHash(Encoding.ASCII.GetBytes(passphrase));
				Array.Resize(ref phash, size);
			}

			return phash;
		}

		public static string Encrypt(string original, string key)
		{
			string encrypted;

			using (var des = new TripleDESCryptoServiceProvider())
			{
				des.Key = GetPassphraseHash(key, des.KeySize / 8);
				des.IV = GetPassphraseHash(key, des.BlockSize / 8);
				des.Padding = PaddingMode.PKCS7;
				des.Mode = CipherMode.ECB;

				byte[] buff = Encoding.ASCII.GetBytes(original);
				encrypted = Convert.ToBase64String(des.CreateEncryptor().TransformFinalBlock(buff, 0, buff.Length));
			}

			return encrypted;
		}

		public static string Decrypt(string encrypted, string key)
		{
			string decrypted;

			using (var des = new TripleDESCryptoServiceProvider())
			{
				des.Key = GetPassphraseHash(key, des.KeySize / 8);
				des.IV = GetPassphraseHash(key, des.BlockSize / 8);
				des.Padding = PaddingMode.PKCS7;
				des.Mode = CipherMode.ECB;

				byte[] buff = Convert.FromBase64String(encrypted);
				decrypted = Encoding.ASCII.GetString(des.CreateDecryptor().TransformFinalBlock(buff, 0, buff.Length));
			}

			return decrypted;
		}
	}
	#endregion

	#region HTML HELPERS
	public enum HtmlInputType
	{
		[Metadata("<input type=\"button\" {0} />")]
		Button,

		[Metadata("<input type=\"checkbox\" {0} />")]
		CheckBox,

		[Metadata("<input type=\"file\" {0} />")]
		File,

		[Metadata("<input type=\"hidden\" {0} />")]
		Hidden,

		[Metadata("<input type=\"image\" {0} />")]
		Image,

		[Metadata("<input type=\"password\" {0} />")]
		Password,

		[Metadata("<input type=\"radio\" {0} />")]
		Radio,

		[Metadata("<input type=\"reset\" {0} />")]
		Reset,

		[Metadata("<input type=\"submit\" {0} />")]
		Submit,

		[Metadata("<input type=\"text\" {0} />")]
		Text,

		[Metadata("<textarea {0}>{1}</textarea>")]
		TextArea
	}

	public enum HtmlFormPostMethod
	{
		Get,
		Post
	}

	#region ABSTRACT BASE HELPER
	public abstract class HtmlBase
	{
		protected Dictionary<string, string> AttribsDict;
		protected Func<string, string>[] AttribsFunc;

		public string CondenseAttribs()
		{
			return (AttribsFunc != null) ? GetParams() : string.Empty;
		}

		private string GetParams()
		{
			var sb = new StringBuilder();
			var attribs = new Dictionary<string, string>();

			if (AttribsFunc != null)
			{
				foreach (var f in AttribsFunc)
				{
					attribs.Add(f.Method.GetParameters()[0].Name == "@class" ? "class" : f.Method.GetParameters()[0].Name, f(null));
				}
			}
			else if (AttribsDict != null)
			{
				attribs = AttribsDict;
			}

			foreach (var kvp in attribs)
			{
				sb.AppendFormat("{0}=\"{1}\" ", kvp.Key, kvp.Value);
			}

			if (sb.Length > 0)
			{
				return sb.ToString().Trim();
			}

			return null;
		}
	}
	#endregion

	#region HTMLTABLE HELPER
	internal enum ColumnTransformType
	{
		New,
		Existing
	}

	public class RowTransform<T> where T : Model
	{
		private readonly List<T> _models;
		private readonly Func<T, string> _func;

		public RowTransform(List<T> models, Func<T, string> func)
		{
			_models = models;
			_func = func;
		}

		public string Result(int index)
		{
			return _func(_models[index]);
		}

		public IEnumerable<string> Results()
		{
			return _models.Select(t => _func(t));
		}
	}

	public class ColumnTransform<T> where T : Model
	{
		private readonly List<T> _models;
		private readonly Func<T, string> _transformFunc;
		internal ColumnTransformType TransformType { get; private set; }

		public string ColumnName { get; private set; }

		public ColumnTransform(List<T> models, string columnName, Func<T, string> transformFunc)
		{
			_models = models;
			_transformFunc = transformFunc;
			ColumnName = columnName;
			PropertyInfo columnInfo = typeof(T).GetProperties().FirstOrDefault(x => x.Name == ColumnName);

			TransformType = columnInfo != null ? ColumnTransformType.Existing : ColumnTransformType.New;
		}

		public string Result(int index)
		{
			return _transformFunc(_models[index]);
		}

		public IEnumerable<string> Results()
		{
			return _models.Select(t => _transformFunc(t));
		}
	}

	public class HtmlTable<T> : HtmlBase where T : Model
	{
		private List<T> _models;
		private List<string> _propertyNames;
		private List<PropertyInfo> _propertyInfos;
		private List<string> _ignoreColumns;
		private List<ColumnTransform<T>> _columnTransforms;
		private List<RowTransform<T>> _rowTransforms;
		public string AlternateRowColor { get; set; }
		public bool AlternateRowColorEnabled { get; set; }

		public HtmlTable(List<T> models, bool alternateRowColorEnabled, params Func<string, string>[] attribs)
		{
			AlternateRowColorEnabled = alternateRowColorEnabled;
			Init(models, null, null, null, attribs);
		}

		public HtmlTable(List<T> models,
										 List<string> ignoreColumns,
										 List<ColumnTransform<T>> columnTransforms,
										 params Func<string, string>[] attribs)
		{
			AlternateRowColorEnabled = true;
			Init(models, ignoreColumns, columnTransforms, null, attribs);
		}

		public HtmlTable(List<T> models,
										 List<string> ignoreColumns,
										 List<ColumnTransform<T>> columnTransforms,
										 List<RowTransform<T>> rowTransforms,
										 params Func<string, string>[] attribs)
		{
			AlternateRowColorEnabled = true;
			Init(models, ignoreColumns, columnTransforms, rowTransforms, attribs);
		}

		private void Init(List<T> models,
											List<string> ignoreColumns,
											List<ColumnTransform<T>> columnTransforms,
											List<RowTransform<T>> rowTransforms,
											params Func<string, string>[] attribs)
		{
			_models = models;

			_ignoreColumns = ignoreColumns;
			AttribsFunc = attribs;
			_columnTransforms = columnTransforms;
			_rowTransforms = rowTransforms;
			AlternateRowColor = "#dddddd";

			_propertyNames = ObtainPropertyNames();
		}

		private List<string> ObtainPropertyNames()
		{
			_propertyNames = new List<string>();
			var hasDescriptiveNames = new List<string>();

			if (_models.Any())
			{
				_propertyInfos = Model.GetPropertiesWithExclusions(_models[0].GetType(), false);

				foreach (var p in _propertyInfos)
				{
					var pn = (DescriptiveNameAttribute)p.GetCustomAttributes(typeof(DescriptiveNameAttribute), false).FirstOrDefault();

					if ((_ignoreColumns != null) && _ignoreColumns.Contains(p.Name))
						continue;

					if (pn != null)
					{
						_propertyNames.Add(pn.Op == DescriptiveNameOperation.SplitCamelCase
							? CultureInfo.InvariantCulture.TextInfo.ToTitleCase(pn.PerformOperation(p.Name))
							: pn.Name);

						hasDescriptiveNames.Add(p.Name);
					}
					else
						_propertyNames.Add(p.Name);
				}

				if (_columnTransforms != null)
				{
					foreach (ColumnTransform<T> addColumn in _columnTransforms)
						if ((!_propertyNames.Contains(addColumn.ColumnName)) && (!hasDescriptiveNames.Contains(addColumn.ColumnName)))
							_propertyNames.Add(addColumn.ColumnName);
				}

				if (_propertyNames.Count > 0)
					return _propertyNames;
			}

			return null;
		}

		public string ToString(int start, int length, bool displayNull)
		{
			if (start > _models.Count() ||
					start < 0 ||
					(length - start) > _models.Count() ||
					(length - start) < 0)
			{
				throw new ArgumentOutOfRangeException("The start or length is out of bounds with the model");
			}

			var html = new StringBuilder();

			html.AppendFormat("<table {0}><thead>", CondenseAttribs());

			foreach (var pn in _propertyNames)
				html.AppendFormat("<th>{0}</th>", pn);

			html.Append("</thead><tbody>");

			for (var i = start; i < length; i++)
			{
				var rowClass = string.Empty;
				var alternatingColor = string.Empty;

				if (_rowTransforms != null)
					foreach (var rt in _rowTransforms)
						rowClass = rt.Result(i);

				if (AlternateRowColorEnabled && !string.IsNullOrEmpty(AlternateRowColor) && (i & 1) != 0)
					alternatingColor = string.Format("bgcolor=\"{0}\"", AlternateRowColor);

				html.AppendFormat("<tr {0} {1}>", rowClass, alternatingColor);

				foreach (var pn in _propertyInfos)
				{
					if ((_ignoreColumns != null) && _ignoreColumns.Contains(pn.Name))
						continue;

					if (pn.CanRead)
					{
						string value;
						var o = pn.GetValue(_models[i], null);
						var sfa = (StringFormatAttribute)Attribute.GetCustomAttribute(pn, typeof(StringFormatAttribute));

						if (sfa != null)
							value = string.Format(sfa.Format, o);
						else
							value = (o == null) ? ((displayNull) ? "NULL" : string.Empty) : o.ToString();

						if (o is DateTime)
						{
							var dfa = (DateFormatAttribute)Attribute.GetCustomAttribute(pn, typeof(DateFormatAttribute));

							if (dfa != null)
								value = ((DateTime)o).ToString(dfa.Format);
						}

						if (_columnTransforms != null)
						{
							var transform = (ColumnTransform<T>)_columnTransforms.FirstOrDefault(x => x.ColumnName == pn.Name && x.TransformType == ColumnTransformType.Existing);

							if (transform != null)
								value = transform.Result(i);
						}

						html.AppendFormat("<td>{0}</td>", value);
					}
				}

				if (_columnTransforms != null)
				{
					foreach (var ct in _columnTransforms.Where(x => x.TransformType == ColumnTransformType.New))
						html.AppendFormat("<td>{0}</td>", ct.Result(i));
				}

				html.Append("</tr>");
			}

			html.Append("</tbody></table>");

			return html.ToString();
		}

		public override string ToString()
		{
			return ToString(0, _models.Count(), false);
		}
	}
	#endregion

	#region CHECKBOX AND RADIO BUTTON LIST (NOT FINISHED)
	public class HtmlCheckBoxList : HtmlBase
	{
		public List<HtmlListItem> Items { get; private set; }

		public HtmlCheckBoxList()
		{
			Items = new List<HtmlListItem>();
		}

		public void AddItem(HtmlListItem item)
		{
			Items.Add(item);
		}

		public string ToString(bool lineBreak)
		{
			var sb = new StringBuilder();
			var counter = 0;
			var br = (lineBreak) ? "<br />" : string.Empty;

			foreach (var i in Items)
			{
				sb.AppendFormat("<input type=\"checkbox\" name=\"checkboxItem{0}\" value=\"{1}\">{2}</input>{3}", counter, i.Value, i.Text, br);
				counter++;
			}

			return sb.ToString();
		}

		public override string ToString()
		{
			return ToString(true);
		}
	}

	public class HtmlRadioButtonList : HtmlBase
	{
		public List<HtmlListItem> Items { get; private set; }

		public HtmlRadioButtonList()
		{
			Items = new List<HtmlListItem>();
		}

		public void AddItem(HtmlListItem item)
		{
			Items.Add(item);
		}

		public string ToString(bool lineBreak)
		{
			var sb = new StringBuilder();
			var counter = 0;
			var br = (lineBreak) ? "<br />" : string.Empty;

			foreach (var i in Items)
			{
				sb.AppendFormat("<input type=\"radio\" name=\"radioItem{0}\" value=\"{1}\">{2}</input>{3}", counter, i.Value, i.Text, br);
				counter++;
			}

			return sb.ToString();
		}

		public override string ToString()
		{
			return ToString(true);
		}
	}
	#endregion

	#region MISC HELPERS
	public class HtmlAnchor : HtmlBase
	{
		private readonly string _url;
		private readonly string _description;

		public HtmlAnchor(string url, string description, params Func<string, string>[] attribs)
		{
			_url = url;
			_description = description;
			AttribsFunc = attribs;
		}

		public override string ToString()
		{
			return string.Format("<a {0} href=\"{1}\">{2}</a>", CondenseAttribs(), _url, _description);
		}
	}

	public class HtmlInput : HtmlBase
	{
		private readonly HtmlInputType _inputType;

		public HtmlInput(HtmlInputType type, params Func<string, string>[] attribs)
		{
			AttribsFunc = attribs;
			_inputType = type;
		}

		public override string ToString()
		{
			return _inputType == HtmlInputType.TextArea ? string.Format(_inputType.GetMetadata(), CondenseAttribs(), string.Empty) : string.Format(_inputType.GetMetadata(), CondenseAttribs());
		}

		public string ToString(string text)
		{
			return _inputType == HtmlInputType.TextArea ? string.Format(_inputType.GetMetadata(), CondenseAttribs(), text) : string.Format(_inputType.GetMetadata(), CondenseAttribs());
		}
	}

	public class HtmlForm : HtmlBase
	{
		private readonly string _action;
		private readonly HtmlFormPostMethod _method;
		private readonly List<string> _inputTags;

		public HtmlForm(string action, HtmlFormPostMethod method, List<string> inputTags, params Func<string, string>[] attribs)
		{
			_action = action;
			_method = method;
			AttribsFunc = attribs;
			_inputTags = inputTags;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendFormat("<form action=\"{0}\" method=\"{1}\" {2}>", _action, _method, CondenseAttribs());

			foreach (var i in _inputTags)
			{
				sb.Append(i);
			}

			sb.Append("</form>");

			return sb.ToString();
		}
	}

	public class HtmlSpan : HtmlBase
	{
		private readonly string _contents;

		public HtmlSpan(string contents, params Func<string, string>[] attribs)
		{
			_contents = contents;
			AttribsFunc = attribs;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendFormat("<span {0}>{1}</span>", CondenseAttribs(), _contents);

			return sb.ToString();
		}
	}

	public class HtmlSelect : HtmlBase
	{
		private readonly List<string> _options;
		private readonly string _selectedDefault;
		private readonly bool _emptyOption;
		private readonly string _enabled;

		public HtmlSelect(List<string> options, string selectedDefault, bool emptyOption, bool enabled, params Func<string, string>[] attribs)
		{
			_options = options;
			AttribsFunc = attribs;
			_selectedDefault = selectedDefault ?? string.Empty;
			_emptyOption = emptyOption;
			_enabled = (enabled) ? "disabled=\"disabled\"" : string.Empty;
		}

		public override string ToString()
		{
			var sb = new StringBuilder();

			sb.AppendFormat("<select {0} {1}>", CondenseAttribs(), _enabled);

			if (_emptyOption)
			{
				sb.Append("<option selected=\"selected\"></option>");
			}

			var count = 0;

			foreach (var o in _options)
			{
				var selected = string.Empty;

				if (!string.IsNullOrEmpty(_selectedDefault) && o == _selectedDefault)
				{
					selected = "selected=\"selected\"";
				}

				sb.AppendFormat("<option name=\"opt{0}\" {1}>{2}</option>", count, selected, o);
				count++;
			}

			sb.Append("</select>");

			return sb.ToString();
		}
	}

	public class HtmlCheckBox : HtmlBase
	{
		private readonly string _id;
		private readonly string _name;
		private readonly string _cssClass;
		private readonly string _check;
		private readonly string _enabled;

		public HtmlCheckBox(string id, string name, string cssClass, bool enabled, bool check)
		{
			_id = id;
			_name = name;
			_cssClass = cssClass;
			_check = (check) ? "checked=\"checked\"" : string.Empty;
			_enabled = (enabled) ? "disabled=\"disabled\"" : string.Empty;
		}

		public override string ToString()
		{
			return string.Format("<input type=\"checkbox\" id=\"{0}\" name=\"{1}\" class=\"{2}\" {3} {4} />", _id, _name, _cssClass, _check, _enabled);
		}
	}

	public class HtmlListItem
	{
		public string Text { get; set; }
		public string Value { get; set; }
	}

	public class HtmlImage : HtmlBase
	{
		public string Src { get; set; }

		public HtmlImage(string src) : this(src, null) { }

		public HtmlImage(string src, params Func<string, string>[] attribs)
		{
			Src = src;
			AttribsFunc = attribs;
		}

		public override string ToString()
		{
			return string.Format("<img src=\"{0}\" {1}/>", Src, CondenseAttribs());
		}
	}

	public class HtmlSimpleList : HtmlBase
	{
		private readonly IEnumerable<string> _data;
		private readonly string _delimiter;

		public HtmlSimpleList(IEnumerable<string> data, string delimiter, params Func<string, string>[] attribs)
		{
			_data = data;
			_delimiter = delimiter;
			AttribsFunc = attribs;
		}

		public override string ToString()
		{
			var result = new StringBuilder();

			result.AppendFormat("<span {0}>", CondenseAttribs());
			result.Append(string.Join(_delimiter, _data));
			result.Append("</span>");

			return result.ToString();
		}
	}
	#endregion

	#region SPECIALIZED HELPERS
	public class HtmlHelperTest : HtmlBase
	{
		public HtmlHelperTest(Controller c)
		{
			const string css = @"
.foobar 
{ 
	margin-top: 10px;
	border: 2px solid red; 
}";

			c.AddHelperBundle("HtmlHelperTest.css", css);
		}

		public override string ToString()
		{
			return "<span class=\"foobar\">Hello, World!</span>";
		}
	}

	public class HtmlUserNameAndPasswordForm : HtmlBase
	{
		private readonly string _applicationTitle;
		private readonly string _loginAlias;

		public HtmlUserNameAndPasswordForm(Controller c, string loginAlias, string applicationTitle)
		{
			_loginAlias = loginAlias;
			_applicationTitle = applicationTitle;

			const string css = @"
.userNameAndPassword {
	margin: 0 auto; 
	width: 155px;  
	text-align: center;  
}
";

			c.AddHelperBundle("HtmlUserNameAndPasswordForm.css", css);
		}

		public override string ToString()
		{
			return
string.Format(@"
<div class=""userNameAndPassword"">
	<h1>{0}</h1>
	<form action=""{1}"" method=""post"">
		<input type=""hidden"" name=""AntiForgeryToken"" value=""%%AntiForgeryToken%%"" />
		<table>
		<tr><td>UserName:<br /><input type=""text"" name=""UserName"" id=""UserName"" /></td></tr>
		<tr><td>Password:<br /><input type=""password"" name=""Password"" id=""Password"" /></td></tr>
		<tr><td><input type=""submit"" value=""Login"" /></td></tr>
		</table>
	</form>
</div>
", _applicationTitle, _loginAlias);
		}
	}
	#endregion
	#endregion

	#region PLUGIN MANAGEMENT

	public enum PluginDevelopmentStatus
	{
		PreAlpha,
		Alpha,
		Beta,
		RC,
		Stable
	}

	public interface IPluginHost
	{
		string HostName { get; }
		string HostVersion { get; }
	}

	public interface IPlugin<T>
	{
		void Load(T host);
		void Unload();
	}

	public abstract class Plugin<T> : IPlugin<T> where T : IPluginHost
	{
		public T Host { get; protected set; }

		public string Guid { get; protected set; }
		public string Name { get; protected set; }
		public string[] Authors { get; protected set; }
		public string Website { get; protected set; }
		public string Version { get; protected set; }
		public PluginDevelopmentStatus DevelopmentStatus { get; protected set; }
		public DateTime DevelopmentDate { get; protected set; }
		public bool Enabled { get; protected set; }
		public string ShortDescription { get; protected set; }
		public string LongDescription { get; protected set; }

		public abstract void Load(T host);
		public abstract void Unload();
	}

	public sealed class PluginManager<T>
	{
		public List<IPlugin<T>> Plugins { get; private set; }
		public T Host { get; private set; }

		public PluginManager(T host)
		{
			Host = host;
		}

		public void LoadPlugin(string path)
		{
			if (Plugins == null)
			{
				Plugins = new List<IPlugin<T>>();
			}

			if (File.Exists(path))
			{
				FileInfo fi = new FileInfo(path);

				if (fi.Extension.Equals(".dll"))
				{
					try
					{
						Assembly pluginAssembly = Assembly.LoadFrom(fi.FullName);

						string ipluginFullName = typeof(IPlugin<>).FullName;

						var pluginsInAssembly = pluginAssembly.GetTypes()
							.Where(x => x.GetInterface(ipluginFullName, false) != null);

						if (pluginsInAssembly.Count() > 0)
						{
							foreach (Type t in pluginsInAssembly)
							{
								if (t.IsPublic && !t.IsAbstract)
								{
									Type ti = t.GetInterface(ipluginFullName, false);

									if (ti != null)
									{
										IPlugin<T> p = (IPlugin<T>)Activator.CreateInstance(pluginAssembly.GetType(t.ToString()));

										p.Load(Host);

										Plugins.Add(p);
									}
									else
									{
										throw new PluginException(string.Format("{0} does not implement the IPlugin interface", t.Name));
									}
								}
							}
						}
						else
						{
							throw new PluginException(string.Format("There are no plugins in {0}", fi.Name));
						}
					}
					catch (Exception ex)
					{
						if (ex is BadImageFormatException || ex is ReflectionTypeLoadException)
						{
							throw new PluginException(string.Format("Unable to load {0}", fi.Name));
						}

						throw;
					}
				}
			}
		}

		public void LoadPlugins(string path)
		{
			if (Directory.Exists(path))
			{
				foreach (FileInfo fi in new DirectoryInfo(path).GetFiles())
				{
					LoadPlugin(fi.FullName);
				}
			}
		}

		public void UnloadPlugins()
		{
			foreach (IPlugin<T> p in Plugins)
			{
				p.Unload();
				Plugins.Remove(p);
			}
		}
	}

	public class PluginException : Exception
	{
		public PluginException(string message)
			: base(message)
		{
		}

		public PluginException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
	#endregion

	#region MASSIVE ORM (FORKED : NON-GPL)
#if MASSIVE
	//
	// New BSD License
	// http://www.opensource.org/licenses/bsd-license.php
	// Copyright (c) 2009, Rob Conery (robconery@gmail.com)
	// All rights reserved.    
	//
	// Redistribution and use in source and binary forms, with or without 
	// modification, are permitted provided that the following conditions are met:
	//
	// Redistributions of source code must retain the above copyright notice, this 
	// list of conditions and the following disclaimer. Redistributions in binary 
	// form must reproduce the above copyright notice, this list of conditions and 
	// the following disclaimer in the documentation and/or other materials provided 
	// with the distribution. Neither the name of the SubSonic nor the names of its 
	// contributors may be used to endorse or promote products derived from this 
	// software without specific prior written permission.
	//
	// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" 
	// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE 
	// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
	// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE 
	// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
	// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
	// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
	// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN 
	// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) 
	// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE 
	// POSSIBILITY OF SUCH DAMAGE.
	//
	namespace Massive
	{
		public static class ObjectExtensions
		{
			/// <summary>
			/// Extension method for adding in a bunch of parameters
			/// </summary>
			public static void AddParams(this DbCommand cmd, params object[] args)
			{
				foreach (var item in args)
					AddParam(cmd, item);
			}

			/// <summary>
			/// Extension for adding single parameter
			/// </summary>
			public static void AddParam(this DbCommand cmd, object item)
			{
				var p = cmd.CreateParameter();
				p.ParameterName = string.Format("@{0}", cmd.Parameters.Count);

				if (item == null)
					p.Value = DBNull.Value;
				else
				{
					if (item.GetType() == typeof(Guid))
					{
						p.Value = item.ToString();
						p.DbType = DbType.String;
						p.Size = 4000;
					}
					else if (item.GetType() == typeof(ExpandoObject))
					{
						var d = (IDictionary<string, object>)item;
						p.Value = d.Values.FirstOrDefault();
					}
					else
						p.Value = item;

					if (item.GetType() == typeof(string))
						p.Size = ((string)item).Length > 4000 ? -1 : 4000;
				}

				cmd.Parameters.Add(p);
			}

			/// <summary>
			/// Turns an IDataReader to a Dynamic list of things
			/// </summary>
			public static List<dynamic> ToExpandoList(this IDataReader rdr)
			{
				var result = new List<dynamic>();

				while (rdr.Read())
					result.Add(rdr.RecordToExpando());

				return result;
			}

			public static dynamic RecordToExpando(this IDataReader rdr)
			{
				dynamic e = new ExpandoObject();
				var d = e as IDictionary<string, object>;

				for (int i = 0; i < rdr.FieldCount; i++)
					d.Add(rdr.GetName(i), DBNull.Value.Equals(rdr[i]) ? null : rdr[i]);

				return e;
			}

			/// <summary>
			/// Turns the object into an ExpandoObject
			/// </summary>
			public static dynamic ToExpando(this object o)
			{
				var result = new ExpandoObject();
				var d = result as IDictionary<string, object>; //work with the Expando as a Dictionary

				if (o.GetType() == typeof(ExpandoObject))
					return o; //shouldn't have to... but just in case

				if (o.GetType() == typeof(NameValueCollection) || o.GetType().IsSubclassOf(typeof(NameValueCollection)))
				{
					var nv = (NameValueCollection)o;
					nv.Cast<string>().Select(key => new KeyValuePair<string, object>(key, nv[key])).ToList().ForEach(i => d.Add(i));
				}
				else
				{
					var props = o.GetType().GetProperties();

					foreach (var item in props)
						d.Add(item.Name, item.GetValue(o, null));
				}

				return result;
			}

			/// <summary>
			/// Turns the object into a Dictionary
			/// </summary>
			public static IDictionary<string, object> ToDictionary(this object thingy)
			{
				return (IDictionary<string, object>)thingy.ToExpando();
			}
		}

		/// <summary>
		/// Convenience class for opening/executing data
		/// </summary>
		public static class DB
		{
			public static DynamicModel Current
			{
				get
				{
					if (ConfigurationManager.ConnectionStrings.Count > 1)
						return new DynamicModel(ConfigurationManager.ConnectionStrings[1].Name);

					throw new InvalidOperationException("Need a connection string name - can't determine what it is");
				}
			}
		}

		/// <summary>
		/// A class that wraps your database table in Dynamic Funtime
		/// </summary>
		public class DynamicModel : DynamicObject
		{
			private DbProviderFactory _factory;
			private string ConnectionString;

			public static DynamicModel Open(string connectionStringName)
			{
				dynamic dm = new DynamicModel(connectionStringName);
				return dm;
			}

			public DynamicModel(string connectionStringName = "", string tableName = "",
					string primaryKeyField = "", string descriptorField = "")
			{
				TableName = tableName == "" ? this.GetType().Name : tableName;
				PrimaryKeyField = string.IsNullOrEmpty(primaryKeyField) ? "ID" : primaryKeyField;
				DescriptorField = descriptorField;

				var _providerName = "System.Data.SqlClient";

				if (!string.IsNullOrEmpty(connectionStringName))
				{
					if (ConfigurationManager.ConnectionStrings[connectionStringName] == null)
						throw new ArgumentException(string.Format("Invalid connection string name: {0}", connectionStringName), "connectionStringName");
				}
				else
				{
					if (!(ConfigurationManager.ConnectionStrings.Count > 1))
						throw new InvalidOperationException("Need a connection string name - can't determine what it is");

					connectionStringName = ConfigurationManager.ConnectionStrings[1].Name;
				}

				if (!string.IsNullOrWhiteSpace(ConfigurationManager.ConnectionStrings[connectionStringName].ProviderName))
					_providerName = ConfigurationManager.ConnectionStrings[connectionStringName].ProviderName;

				_factory = DbProviderFactories.GetFactory(_providerName);
				ConnectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
			}

			/// <summary>
			/// Creates a new Expando from a Form POST - white listed against the columns in the DB
			/// </summary>
			public dynamic CreateFrom(NameValueCollection coll)
			{
				dynamic result = new ExpandoObject();
				var dc = (IDictionary<string, object>)result;
				var schema = Schema;

				//loop the collection, setting only what's in the Schema
				foreach (var item in coll.Keys)
				{
					var exists = schema.Any(x => x.COLUMN_NAME.ToLower() == item.ToString().ToLower());

					if (exists)
					{
						var key = item.ToString();
						var val = coll[key];
						dc.Add(key, val);
					}
				}

				return result;
			}

			/// <summary>
			/// Gets a default value for the column
			/// </summary>
			public dynamic DefaultValue(dynamic column)
			{
				dynamic result = null;
				string def = column.COLUMN_DEFAULT;

				if (String.IsNullOrEmpty(def))
					result = null;
				else if (def == "getdate()" || def == "(getdate())")
					result = DateTime.Now.ToShortDateString();
				else if (def == "newid()")
					result = Guid.NewGuid().ToString();
				else
					result = def.Replace("(", "").Replace(")", "");

				return result;
			}

			/// <summary>
			/// Creates an empty Expando set with defaults from the DB
			/// </summary>
			public dynamic Prototype
			{
				get
				{
					dynamic result = new ExpandoObject();
					var schema = Schema;

					foreach (dynamic column in schema)
					{
						var dc = (IDictionary<string, object>)result;
						dc.Add(column.COLUMN_NAME, DefaultValue(column));
					}

					result._Table = this;

					return result;
				}
			}

			public string DescriptorField { get; protected set; }

			/// <summary>
			/// List out all the schema bits for use with ... whatever
			/// </summary>
			IEnumerable<dynamic> _schema;
			public IEnumerable<dynamic> Schema
			{
				get
				{
					if (_schema == null)
						_schema = Query("SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @0", TableName);

					return _schema;
				}
			}

			/// <summary>
			/// Enumerates the reader yielding the result - thanks to Jeroen Haegebaert
			/// </summary>
			public virtual IEnumerable<dynamic> Query(string sql, params object[] args)
			{
				using (var conn = OpenConnection())
				{
					var rdr = CreateCommand(sql, conn, args).ExecuteReader();

					while (rdr.Read())
						yield return rdr.RecordToExpando();
				}
			}

			public virtual IEnumerable<dynamic> Query(string sql, DbConnection connection, params object[] args)
			{
				using (var rdr = CreateCommand(sql, connection, args).ExecuteReader())
				{
					while (rdr.Read())
						yield return rdr.RecordToExpando();
				}
			}

			/// <summary>
			/// Returns a single result
			/// </summary>
			public virtual object Scalar(string sql, params object[] args)
			{
				object result = null;

				using (var conn = OpenConnection())
					result = CreateCommand(sql, conn, args).ExecuteScalar();

				return result;
			}

			/// <summary>
			/// Creates a DBCommand that you can use for loving your database.
			/// </summary>
			DbCommand CreateCommand(string sql, DbConnection conn, params object[] args)
			{
				var result = _factory.CreateCommand();
				result.Connection = conn;
				result.CommandText = sql;

				if (args.Length > 0)
					result.AddParams(args);

				return result;
			}

			/// <summary>
			/// Returns and OpenConnection
			/// </summary>
			public virtual DbConnection OpenConnection()
			{
				var result = _factory.CreateConnection();
				result.ConnectionString = ConnectionString;
				result.Open();

				return result;
			}

			/// <summary>
			/// Builds a set of Insert and Update commands based on the passed-on objects.
			/// These objects can be POCOs, Anonymous, NameValueCollections, or Expandos. Objects
			/// With a PK property (whatever PrimaryKeyField is set to) will be created at UPDATEs
			/// </summary>
			public virtual List<DbCommand> BuildCommands(params object[] things)
			{
				var commands = new List<DbCommand>();

				foreach (var item in things)
				{
					if (HasPrimaryKey(item))
						commands.Add(CreateUpdateCommand(item.ToExpando(), GetPrimaryKey(item)));
					else
						commands.Add(CreateInsertCommand(item.ToExpando()));
				}

				return commands;
			}

			public virtual int Execute(DbCommand command)
			{
				return Execute(new DbCommand[] { command });
			}

			public virtual int Execute(string sql, params object[] args)
			{
				return Execute(CreateCommand(sql, null, args));
			}

			/// <summary>
			/// Executes a series of DBCommands in a transaction
			/// </summary>
			public virtual int Execute(IEnumerable<DbCommand> commands)
			{
				var result = 0;

				using (var conn = OpenConnection())
				{
					using (var tx = conn.BeginTransaction())
					{
						foreach (var cmd in commands)
						{
							cmd.Connection = conn;
							cmd.Transaction = tx;
							result += cmd.ExecuteNonQuery();
						}

						tx.Commit();
					}
				}

				return result;
			}

			public virtual string PrimaryKeyField { get; set; }

			/// <summary>
			/// Conventionally introspects the object passed in for a field that 
			/// looks like a PK. If you've named your PrimaryKeyField, this becomes easy
			/// </summary>
			public virtual bool HasPrimaryKey(object o)
			{
				return o.ToDictionary().ContainsKey(PrimaryKeyField);
			}

			/// <summary>
			/// If the object passed in has a property with the same name as your PrimaryKeyField
			/// it is returned here.
			/// </summary>
			public virtual object GetPrimaryKey(object o)
			{
				object result = null;
				o.ToDictionary().TryGetValue(PrimaryKeyField, out result);

				return result;
			}

			public virtual string TableName { get; set; }

			/// <summary>
			/// Returns all records complying with the passed-in WHERE clause and arguments, 
			/// ordered as specified, limited (TOP) by limit.
			/// </summary>
			public virtual IEnumerable<dynamic> All(string where = "", string orderBy = "", int limit = 0, string columns = "*", params object[] args)
			{
				string sql = BuildSelect(where, orderBy, limit);

				return Query(string.Format(sql, columns, TableName), args);
			}

			private static string BuildSelect(string where, string orderBy, int limit)
			{
				string sql = limit > 0 ? "SELECT TOP " + limit + " {0} FROM {1} " : "SELECT {0} FROM {1} ";

				if (!string.IsNullOrEmpty(where))
					sql += where.Trim().StartsWith("where", StringComparison.OrdinalIgnoreCase) ? where : " WHERE " + where;

				if (!String.IsNullOrEmpty(orderBy))
					sql += orderBy.Trim().StartsWith("order by", StringComparison.OrdinalIgnoreCase) ? orderBy : " ORDER BY " + orderBy;

				return sql;
			}

			/// <summary>
			/// Returns a dynamic PagedResult. Result properties are Items, TotalPages, and TotalRecords.
			/// </summary>
			public virtual dynamic Paged(string where = "", string orderBy = "", string columns = "*", int pageSize = 20, int currentPage = 1, params object[] args)
			{
				return BuildPagedResult(where: where, orderBy: orderBy, columns: columns, pageSize: pageSize, currentPage: currentPage, args: args);
			}

			public virtual dynamic Paged(string sql, string primaryKey, string where = "", string orderBy = "", string columns = "*", int pageSize = 20, int currentPage = 1, params object[] args)
			{
				return BuildPagedResult(sql, primaryKey, where, orderBy, columns, pageSize, currentPage, args);
			}

			private dynamic BuildPagedResult(string sql = "", string primaryKeyField = "", string where = "", string orderBy = "", string columns = "*", int pageSize = 20, int currentPage = 1, params object[] args)
			{
				dynamic result = new ExpandoObject();
				var countSQL = "";

				if (!string.IsNullOrEmpty(sql))
					countSQL = string.Format("SELECT COUNT({0}) FROM ({1}) AS PagedTable", primaryKeyField, sql);
				else
					countSQL = string.Format("SELECT COUNT({0}) FROM {1}", PrimaryKeyField, TableName);

				if (String.IsNullOrEmpty(orderBy))
					orderBy = string.IsNullOrEmpty(primaryKeyField) ? PrimaryKeyField : primaryKeyField;

				if (!string.IsNullOrEmpty(where))
					if (!where.Trim().StartsWith("where", StringComparison.CurrentCultureIgnoreCase))
						where = " WHERE " + where;

				var query = "";
				if (!string.IsNullOrEmpty(sql))
					query = string.Format("SELECT {0} FROM (SELECT ROW_NUMBER() OVER (ORDER BY {1}) AS Row, {0} FROM ({2}) AS PagedTable {3}) AS Paged ", columns, orderBy, sql, where);
				else
					query = string.Format("SELECT {0} FROM (SELECT ROW_NUMBER() OVER (ORDER BY {1}) AS Row, {0} FROM {2} {3}) AS Paged ", columns, orderBy, TableName, where);

				var pageStart = (currentPage - 1) * pageSize;
				query += string.Format(" WHERE Row > {0} AND Row <={1}", pageStart, (pageStart + pageSize));
				countSQL += where;
				result.TotalRecords = Scalar(countSQL, args);
				result.TotalPages = result.TotalRecords / pageSize;

				if (result.TotalRecords % pageSize > 0)
					result.TotalPages += 1;

				result.Items = Query(string.Format(query, columns, TableName), args);

				return result;
			}

			/// <summary>
			/// Returns a single row from the database
			/// </summary>
			public virtual dynamic Single(string where, params object[] args)
			{
				var sql = string.Format("SELECT * FROM {0} WHERE {1}", TableName, where);

				return Query(sql, args).FirstOrDefault();
			}

			/// <summary>
			/// Returns a single row from the database
			/// </summary>
			public virtual dynamic Single(object key, string columns = "*")
			{
				var sql = string.Format("SELECT {0} FROM {1} WHERE {2} = @0", columns, TableName, PrimaryKeyField);

				return Query(sql, key).FirstOrDefault();
			}

			/// <summary>
			/// This will return a string/object dictionary for dropdowns etc
			/// </summary>
			public virtual IDictionary<string, object> KeyValues(string orderBy = "")
			{
				if (String.IsNullOrEmpty(DescriptorField))
					throw new InvalidOperationException("There's no DescriptorField set - do this in your constructor to describe the text value you want to see");

				var sql = string.Format("SELECT {0},{1} FROM {2} ", PrimaryKeyField, DescriptorField, TableName);

				if (!String.IsNullOrEmpty(orderBy))
					sql += "ORDER BY " + orderBy;

				var results = Query(sql).ToList().Cast<IDictionary<string, object>>();

				return results.ToDictionary(key => key[PrimaryKeyField].ToString(), value => value[DescriptorField]);
			}

			/// <summary>
			/// This will return an Expando as a Dictionary
			/// </summary>
			public virtual IDictionary<string, object> ItemAsDictionary(ExpandoObject item)
			{
				return (IDictionary<string, object>)item;
			}

			//Checks to see if a key is present based on the passed-in value
			public virtual bool ItemContainsKey(string key, ExpandoObject item)
			{
				var dc = ItemAsDictionary(item);

				return dc.ContainsKey(key);
			}

			/// <summary>
			/// Executes a set of objects as Insert or Update commands based on their property settings, within a transaction.
			/// These objects can be POCOs, Anonymous, NameValueCollections, or Expandos. Objects
			/// With a PK property (whatever PrimaryKeyField is set to) will be created at UPDATEs
			/// </summary>
			public virtual int Save(params object[] things)
			{
				foreach (var item in things)
					if (!IsValid(item))
						throw new InvalidOperationException("Can't save this item: " + String.Join("; ", Errors.ToArray()));

				var commands = BuildCommands(things);

				return Execute(commands);
			}

			public virtual DbCommand CreateInsertCommand(dynamic expando)
			{
				DbCommand result = null;
				var settings = (IDictionary<string, object>)expando;
				var sbKeys = new StringBuilder();
				var sbVals = new StringBuilder();
				var stub = "INSERT INTO {0} ({1}) \r\n VALUES ({2})";
				result = CreateCommand(stub, null);
				int counter = 0;

				foreach (var item in settings)
				{
					sbKeys.AppendFormat("[{0}],", item.Key);
					sbVals.AppendFormat("@{0},", counter.ToString());
					result.AddParam(item.Value);
					counter++;
				}

				if (counter > 0)
				{
					var keys = sbKeys.ToString().Substring(0, sbKeys.Length - 1);
					var vals = sbVals.ToString().Substring(0, sbVals.Length - 1);
					var sql = string.Format(stub, TableName, keys, vals);
					result.CommandText = sql;
				}
				else
					throw new InvalidOperationException("Can't parse this object to the database - there are no properties set");

				return result;
			}

			/// <summary>
			/// Creates a command for use with transactions - internal stuff mostly, but here for you to play with
			/// </summary>
			public virtual DbCommand CreateUpdateCommand(dynamic expando, object key)
			{
				var settings = (IDictionary<string, object>)expando;
				var sbKeys = new StringBuilder();
				var stub = "UPDATE {0} SET {1} WHERE {2} = @{3}";
				var args = new List<object>();
				var result = CreateCommand(stub, null);
				int counter = 0;

				foreach (var item in settings)
				{
					var val = item.Value;

					if (!item.Key.Equals(PrimaryKeyField, StringComparison.OrdinalIgnoreCase) && item.Value != null)
					{
						result.AddParam(val);
						sbKeys.AppendFormat("[{0}] = @{1}, \r\n", item.Key, counter.ToString());
						counter++;
					}
				}
				if (counter > 0)
				{
					//add the key
					result.AddParam(key);
					//strip the last commas
					var keys = sbKeys.ToString().Substring(0, sbKeys.Length - 4);
					result.CommandText = string.Format(stub, TableName, keys, PrimaryKeyField, counter);
				}
				else
					throw new InvalidOperationException("No parsable object was sent in - could not divine any name/value pairs");

				return result;
			}

			/// <summary>
			/// Removes one or more records from the DB according to the passed-in WHERE
			/// </summary>
			public virtual DbCommand CreateDeleteCommand(string where = "", object key = null, params object[] args)
			{
				var sql = string.Format("DELETE FROM {0} ", TableName);

				if (key != null)
				{
					sql += string.Format("WHERE [{0}]=@0", PrimaryKeyField);
					args = new object[] { key };
				}
				else if (!string.IsNullOrEmpty(where))
					sql += where.Trim().StartsWith("where", StringComparison.OrdinalIgnoreCase) ? where : "WHERE " + where;

				return CreateCommand(sql, null, args);
			}

			public bool IsValid(dynamic item)
			{
				Errors.Clear();
				Validate(item);

				return Errors.Count == 0;
			}

			//Temporary holder for error messages
			public IList<string> Errors = new List<string>();

			/// <summary>
			/// Adds a record to the database. You can pass in an Anonymous object, an ExpandoObject,
			/// A regular old POCO, or a NameValueColletion from a Request.Form or Request.QueryString
			/// </summary>
			public virtual dynamic Insert(object o)
			{
				var ex = o.ToExpando();

				if (!IsValid(ex))
					throw new InvalidOperationException("Can't insert: " + String.Join("; ", Errors.ToArray()));

				if (BeforeSave(ex))
				{
					using (dynamic conn = OpenConnection())
					{
						var cmd = CreateInsertCommand(ex);
						cmd.Connection = conn;
						cmd.ExecuteNonQuery();
						//cmd.CommandText = "SELECT SCOPE_IDENTITY() as newID";
						cmd.CommandText = "SELECT @@IDENTITY as newID";
						ex.ID = cmd.ExecuteScalar();
						Inserted(ex);
					}

					return ex;
				}

				return null;
			}

			/// <summary>
			/// Updates a record in the database. You can pass in an Anonymous object, an ExpandoObject,
			/// A regular old POCO, or a NameValueCollection from a Request.Form or Request.QueryString
			/// </summary>
			public virtual int Update(object o, object key)
			{
				var ex = o.ToExpando();

				if (!IsValid(ex))
					throw new InvalidOperationException("Can't Update: " + String.Join("; ", Errors.ToArray()));

				var result = 0;

				if (BeforeSave(ex))
				{
					result = Execute(CreateUpdateCommand(ex, key));
					Updated(ex);
				}

				return result;
			}

			/// <summary>
			/// Removes one or more records from the DB according to the passed-in WHERE
			/// </summary>
			public int Delete(object key = null, string where = "", params object[] args)
			{
				var deleted = this.Single(key);
				var result = 0;

				if (BeforeDelete(deleted))
				{
					result = Execute(CreateDeleteCommand(where: where, key: key, args: args));
					Deleted(deleted);
				}

				return result;
			}

			public void DefaultTo(string key, object value, dynamic item)
			{
				if (!ItemContainsKey(key, item))
				{
					var dc = (IDictionary<string, object>)item;
					dc[key] = value;
				}
			}

			//Hooks
			public virtual void Validate(dynamic item) { }
			public virtual void Inserted(dynamic item) { }
			public virtual void Updated(dynamic item) { }
			public virtual void Deleted(dynamic item) { }
			public virtual bool BeforeDelete(dynamic item) { return true; }
			public virtual bool BeforeSave(dynamic item) { return true; }

			//validation methods
			public virtual void ValidatesPresenceOf(object value, string message = "Required")
			{
				if (value == null)
					Errors.Add(message);

				if (String.IsNullOrEmpty(value.ToString()))
					Errors.Add(message);
			}

			//fun methods
			public virtual void ValidatesNumericalityOf(object value, string message = "Should be a number")
			{
				var type = value.GetType().Name;
				var numerics = new string[] { "Int32", "Int16", "Int64", "Decimal", "Double", "Single", "Float" };

				if (!numerics.Contains(type))
					Errors.Add(message);
			}

			public virtual void ValidateIsCurrency(object value, string message = "Should be money")
			{
				if (value == null)
					Errors.Add(message);

				decimal val = decimal.MinValue;
				decimal.TryParse(value.ToString(), out val);

				if (val == decimal.MinValue)
					Errors.Add(message);
			}

			public int Count()
			{
				return Count(TableName);
			}

			public int Count(string tableName, string where = "")
			{
				return (int)Scalar("SELECT COUNT(*) FROM " + tableName + " " + where);
			}

			/// <summary>
			/// A helpful query tool
			/// </summary>
			public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
			{
				//parse the method
				var constraints = new List<string>();
				var counter = 0;
				var info = binder.CallInfo;

				// accepting named args only... SKEET!
				if (info.ArgumentNames.Count != args.Length)
					throw new InvalidOperationException("Please use named arguments for this type of query - the column name, orderby, columns, etc");

				//first should be "FindBy, Last, Single, First"
				var op = binder.Name;
				var columns = " * ";
				string orderBy = string.Format(" ORDER BY {0}", PrimaryKeyField);
				string sql = "";
				string where = "";
				var whereArgs = new List<object>();

				//loop the named args - see if we have order, columns and constraints
				if (info.ArgumentNames.Count > 0)
				{
					for (int i = 0; i < args.Length; i++)
					{
						var name = info.ArgumentNames[i].ToLower();
						switch (name)
						{
							case "orderby":
								orderBy = " ORDER BY " + args[i];
								break;
							case "columns":
								columns = args[i].ToString();
								break;
							default:
								constraints.Add(string.Format(" {0} = @{1}", name, counter));
								whereArgs.Add(args[i]);
								counter++;
								break;
						}
					}
				}

				//Build the WHERE bits
				if (constraints.Count > 0)
					where = " WHERE " + string.Join(" AND ", constraints.ToArray());

				//probably a bit much here but... yeah this whole thing needs to be refactored...
				if (op.ToLower() == "count")
					result = Scalar("SELECT COUNT(*) FROM " + TableName + where, whereArgs.ToArray());
				else if (op.ToLower() == "sum")
					result = Scalar("SELECT SUM(" + columns + ") FROM " + TableName + where, whereArgs.ToArray());
				else if (op.ToLower() == "max")
					result = Scalar("SELECT MAX(" + columns + ") FROM " + TableName + where, whereArgs.ToArray());
				else if (op.ToLower() == "min")
					result = Scalar("SELECT MIN(" + columns + ") FROM " + TableName + where, whereArgs.ToArray());
				else if (op.ToLower() == "avg")
					result = Scalar("SELECT AVG(" + columns + ") FROM " + TableName + where, whereArgs.ToArray());
				else
				{
					//build the SQL
					var justOne = op.StartsWith("First") || op.StartsWith("Last") || op.StartsWith("Get") || op.StartsWith("Single");

					if (justOne)
					{
						sql = "SELECT TOP 1 " + columns + " FROM " + TableName + where;

						//Be sure to sort by DESC on the PK (PK Sort is the default)
						if (op.StartsWith("Last"))
						{
							orderBy = orderBy + " DESC ";
						}

						//return a single record
						result = Query(sql + orderBy, whereArgs.ToArray()).ToArray().FirstOrDefault();
					}
					else
					{
						//default to multiple
						sql = "SELECT " + columns + " FROM " + TableName + where;

						//return lots
						result = Query(sql + orderBy, whereArgs.ToArray());
					}
				}

				return true;
			}
		}
	}
#endif
	#endregion

	#region ACTIVE DIRECTORY
#if ACTIVEDIRECTORY
	#region WEB.CONFIG
	public class ActiveDirectoryWebConfig : ConfigurationSection
	{
		[ConfigurationProperty("EncryptionKey", DefaultValue = "", IsRequired = false)]
		public string EncryptionKey
		{
			get { return this["EncryptionKey"] as string; }
		}

		[ConfigurationProperty("UserName", DefaultValue = null, IsRequired = false)]
		public string AdSearchUser
		{
			get { return this["UserName"].ToString(); }
			set { this["UserName"] = value; }
		}

		[ConfigurationProperty("Password", DefaultValue = null, IsRequired = false)]
		public string AdSearchPw
		{
			get { return this["Password"].ToString(); }
			set { this["Password"] = value; }
		}

		[ConfigurationProperty("Domain", DefaultValue = null, IsRequired = false)]
		public string AdSearchDomain
		{
			get { return this["Domain"].ToString(); }
			set { this["Domain"] = value; }
		}

		[ConfigurationProperty("SearchRoot", DefaultValue = null, IsRequired = false)]
		public string AdSearchRoot
		{
			get { return this["SearchRoot"].ToString(); }
			set { this["SearchRoot"] = value; }
		}
	}
	#endregion

	public class ActiveDirectoryUser
	{
		public string FirstName { get; internal set; }
		public string LastName { get; internal set; }
		public string DisplayName { get; internal set; }
		public string UserName { get; internal set; }
		public string UserPrincipalName { get; internal set; }
		public string PrimaryEmailAddress { get; internal set; }
		public string PhoneNumber { get; internal set; }
		public string Path { get; internal set; }
		public X509Certificate2 ClientCertificate { get; internal set; }
	}

	public static class ActiveDirectory
	{
		public static ActiveDirectoryWebConfig WebConfig = ConfigurationManager.GetSection("ActiveDirectory") as ActiveDirectoryWebConfig;
		public static string AdSearchUser = (WebConfig == null) ? null : (!string.IsNullOrEmpty(WebConfig.AdSearchUser) && !string.IsNullOrEmpty(WebConfig.EncryptionKey)) ? Encryption.Decrypt(WebConfig.AdSearchUser, WebConfig.EncryptionKey) : null;
		public static string AdSearchPw = (WebConfig == null) ? null : (!string.IsNullOrEmpty(WebConfig.AdSearchPw) && !string.IsNullOrEmpty(WebConfig.EncryptionKey)) ? Encryption.Decrypt(WebConfig.AdSearchPw, WebConfig.EncryptionKey) : null;
		public static string AdSearchDomain = (WebConfig == null) ? null : WebConfig.AdSearchDomain;

		public static ActiveDirectoryUser LookupUserByUpn(string upn)
		{
			var ctx = new PrincipalContext(ContextType.Domain, AdSearchDomain, AdSearchUser, AdSearchPw);
			var usr = UserPrincipal.FindByIdentity(ctx, IdentityType.UserPrincipalName, upn);
			ActiveDirectoryUser adUser = null;

			if (usr != null)
			{
				var de = usr.GetUnderlyingObject() as DirectoryEntry;

				adUser = GetUser(de);
			}

			return adUser;
		}

		private static ActiveDirectoryUser GetUser(DirectoryEntry de)
		{
			return new ActiveDirectoryUser()
			{
				FirstName = de.Properties["givenName"].Value.ToString(),
				LastName = de.Properties["sn"].Value.ToString(),
				UserPrincipalName = (de.Properties["userPrincipalName"].Value != null) ?
							de.Properties["userPrincipalName"].Value.ToString() : null,
				DisplayName = de.Properties["displayName"].Value.ToString(),
				UserName = (de.Properties["samAccountName"].Value != null) ? de.Properties["samAccountName"].Value.ToString() : null,
				PrimaryEmailAddress = GetPrimarySmtp(de) ?? string.Empty,
				PhoneNumber = de.Properties["telephoneNumber"].Value.ToString(),
				Path = de.Path,
				ClientCertificate = de.Properties.Contains("userSMIMECertificate") ?
								new X509Certificate2(de.Properties["userSMIMECertificate"].Value as byte[]) ?? null :
								new X509Certificate2(de.Properties["userCertificate"].Value as byte[]) ?? null
			};
		}

		private static List<string> GetProxyAddresses(DirectoryEntry user)
		{
			var addresses = new List<string>();

			if (user.Properties.Contains("proxyAddresses"))
				addresses.AddRange(from string addr in user.Properties["proxyAddresses"] select Regex.Replace(addr, @"\s+", string.Empty, RegexOptions.IgnoreCase).Trim());

			return addresses;
		}

		private static string GetPrimarySmtp(DirectoryEntry user)
		{
			return (from p in GetProxyAddresses(user) where p.StartsWith("SMTP:", StringComparison.Ordinal) select p.Replace("SMTP:", string.Empty).ToLowerInvariant()).FirstOrDefault();
		}
	}

	public class ActiveDirectoryAuthenticationEventArgs : EventArgs
	{
		public ActiveDirectoryUser User { get; set; }
		public bool Authenticated { get; set; }
		public string CACID { get; set; }
	}

	public class ActiveDirectoryAuthentication : IBoundToAction
	{
		private Controller _controller;
		public ActiveDirectoryUser User { get; private set; }
		public bool Authenticated { get; private set; }
		public string CACID { get; private set; }

		private event EventHandler<ActiveDirectoryAuthenticationEventArgs> ActiveDirectoryLookupEvent = (sender, args) => { };

		public ActiveDirectoryAuthentication()
		{
		}

		public ActiveDirectoryAuthentication(EventHandler<ActiveDirectoryAuthenticationEventArgs> activeDirectoryLookupHandler)
		{
			ActiveDirectoryLookupEvent += activeDirectoryLookupHandler;
		}

		public void Initialize(RouteInfo routeInfo)
		{
			ActiveDirectoryLookupEvent.ThrowIfArgumentNull();

			_controller = routeInfo.Controller;

			Authenticate();
		}

		public string GetCACIDFromCN()
		{
			if (_controller.ClientCertificate == null)
				throw new Exception("The HttpContext.Request.ClientCertificate did not contain a valid certificate");

			var cn = _controller.ClientCertificate.GetNameInfo(X509NameType.SimpleName, false);
			var cacid = string.Empty;
			var valid = true;

			if (string.IsNullOrEmpty(cn))
				throw new Exception("Cannot determine the simple name from the client certificate");

			if (cn.Contains("."))
			{
				var fields = cn.Split('.');

				if (fields.Length > 0)
				{
					cacid = fields[fields.Length - 1];

					if (cacid.ToCharArray().Any(c => !Char.IsDigit(c)))
						valid = false;
				}
			}

			if (valid)
				return cacid;
			else
				throw new Exception(string.Format("The CAC ID was not in the expected format within the common name (last.first.middle.cacid), actual CN = {0}", cn));
		}

		public void Authenticate()
		{
			var args = new ActiveDirectoryAuthenticationEventArgs();

#if DEBUG
			ActiveDirectoryLookupEvent(this, args);

			User = args.User;
			Authenticated = args.Authenticated;
			CACID = args.CACID;
#else
			CACID = GetCACIDFromCN();

			User = null;
			Authenticated = false;

			if (!String.IsNullOrEmpty(CACID))
			{
				var chain = new X509Chain
				{
					ChainPolicy =
					{
						RevocationFlag = X509RevocationFlag.EntireChain,
						RevocationMode = X509RevocationMode.Online,
						UrlRetrievalTimeout = new TimeSpan(0, 0, 30)
					}
				};

				if (chain.Build(_controller.ClientCertificate))
				{
					try
					{
						args.CACID = CACID;

						ActiveDirectoryLookupEvent(this, args);

						if (args.User != null)
						{
							User = args.User;
							Authenticated = true;
						}
					}
					catch (DirectoryServicesCOMException)
					{
						throw new Exception("A problem occurred trying to communicate with Active Directory");
					}
				}
			}
#endif
		}
	}
#endif
	#endregion

	#region OPENAUTH
#if OPENAUTH
	public class OpenAuthClaimsResponse
	{
		public string ClaimedIdentifier { get; internal set; }
		public string FullName { get; internal set; }
		public string Email { get; internal set; }
	}

	public static class OpenAuth
	{
		public static void LogonViaOpenAuth(string identifier, Uri requestUrl, Action<Uri> providerUri, Action<string> invalidLogon)
		{
			identifier.ThrowIfArgumentNull();
			invalidLogon.ThrowIfArgumentNull();

			if (!Identifier.IsValid(identifier))
			{
				if (invalidLogon != null)
					invalidLogon("The specified login identifier is invalid");
			}
			else
			{
				using (var openid = new OpenIdRelyingParty())
				{
					//ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;

					var uriBuilder = new UriBuilder(requestUrl) { Query = "" };
					var request = openid.CreateRequest(Identifier.Parse(identifier), Realm.AutoDetect, uriBuilder.Uri);

					providerUri(request.Provider.Uri);

					request.AddExtension(new ClaimsRequest
					{
						Email = DemandLevel.Require,
						FullName = DemandLevel.Require,
						Nickname = DemandLevel.Request
					});

					request.RedirectToProvider();
				}
			}
		}

		public static void FinalizeLogonViaOpenAuth(Uri providerUri, Action<OpenAuthClaimsResponse> authenticated, Action<string> cancelled, Action<string> failed)
		{
			authenticated.ThrowIfArgumentNull();
			cancelled.ThrowIfArgumentNull();
			failed.ThrowIfArgumentNull();

			using (var openid = new OpenIdRelyingParty())
			{
				var response = openid.GetResponse();

				if (response != null)
				{
					if (providerUri != response.Provider.Uri)
						throw new Exception("The request OpenID provider Uri does not match the response Uri");

					switch (response.Status)
					{
						case AuthenticationStatus.Authenticated:

							var claimsResponse = response.GetExtension<ClaimsResponse>();

							if (claimsResponse != null)
							{
								if (string.IsNullOrEmpty(claimsResponse.Email))
									throw new Exception("The open auth provider did not return a claims response with a valid email address");

								var openAuthClaimsResponse = new OpenAuthClaimsResponse()
								{
									Email = claimsResponse.Email,
									FullName = claimsResponse.FullName,
									ClaimedIdentifier = response.ClaimedIdentifier,
								};

								if (authenticated != null)
									authenticated(openAuthClaimsResponse);
							}
							break;

						case AuthenticationStatus.Canceled:
							if (cancelled != null)
								cancelled("Login was cancelled at the provider");
							break;

						case AuthenticationStatus.Failed:
							if (failed != null)
								failed("Login failed using the provided OpenID identifier");
							break;
					}
				}
			}
		}
	}
#endif
	#endregion

	#region GRAVATAR (FORKED : NON-GPL)
#if GRAVATAR
	// https://github.com/runeborg
	//
	// "Gravatar wrapper for ASP.NET MVC. Feel free to use it any way you want."
	//    
	namespace Gravatar
	{
		/// <summary>
		/// Specifies what displays if the email has no matching Gravatar image.
		/// </summary>
		public enum DefaultGravatar
		{
			/// <summary>
			/// Use the default image (Gravatar logo)
			/// </summary>
			GravatarLogo,
			/// <summary>
			/// Do not load any image if none is associated with the email, instead return an HTTP 404 (File Not Found) response.
			/// </summary>
			None,
			/// <summary>
			/// A simple, cartoon-style silhouetted outline of a person (does not vary by email).
			/// </summary>
			MysteryMan,
			/// <summary>
			/// A geometric pattern based on an email.
			/// </summary>
			IdentIcon,
			/// <summary>
			/// A generated 'monster' with different colors, faces, etc.
			/// </summary>
			MonsterId,
			/// <summary>
			/// Generated faces with differing features and backgrounds.
			/// </summary>
			Wavatar,
			/// <summary>
			/// Generated, 8-bit arcade-style pixelated faces.
			/// </summary>
			Retro
		}

		/// <summary>
		/// If the requested email hash does not have an image meeting the requested rating level, then the default image is returned (or the specified default).
		/// </summary>
		public enum GravatarRating
		{
			/// <summary>
			///  Default rating (G)
			/// </summary>
			Default,
			/// <summary>
			/// Suitable for display on all websites with any audience type.
			/// </summary>
			G,
			/// <summary>
			/// May contain rude gestures, provocatively dressed individuals, the lesser swear words, or mild violence.
			/// </summary>
			PG,
			/// <summary>
			/// May contain such things as harsh profanity, intense violence, nudity, or hard drug use.
			/// </summary>
			R,
			/// <summary>
			/// May contain hardcore sexual imagery or extremely disturbing violence.
			/// </summary>
			X
		}

		/// <summary>
		/// Generates a Gravatar url
		/// </summary>
		public class GravatarGenerator
		{
			/// <summary>
			/// Email to generate a Gravatar image for
			/// </summary>
			private string _email { get; set; }
			/// <summary>
			/// The size of the image in pixels. Defaults to 80px
			/// </summary>
			private int _size { get; set; }
			/// <summary>
			/// A default image to fall back to.
			/// </summary>
			private string _defaultImage { get; set; }
			/// <summary>
			/// Wether to append a file ending to the url (.jpg).
			/// </summary>
			private bool _appendFileType { get; set; }
			/// <summary>
			/// Force the default image to display.
			/// </summary>
			private bool _forceDefaultImage { get; set; }
			/// <summary>
			/// Image rating to display for. See <see cref="Gravatar.GravatarRating"/> for details.
			/// </summary>
			private GravatarRating _displayRating { get; set; }
			/// <summary>
			/// How to generate a default image if no gravatar exists for the email. See <see cref="Gravatar.DefaultGravatar"/> for details.
			/// </summary>
			private DefaultGravatar _defaultDisplay { get; set; }
			/// <summary>
			/// If https should be used.
			/// </summary>
			private bool _useHttps { get; set; }

			/// <summary>
			/// Creates a GravatarGenerator.
			/// </summary>
			/// <param name="email">Email to generate Gravatar for.</param>
			/// <param name="useHttps">Wether to use https or not.</param>
			public GravatarGenerator(string email, bool useHttps)
			{
				_email = email;
				_useHttps = useHttps;
			}

			/// <summary>
			/// Gets the Url for the Gravatar
			/// </summary>
			public string Url
			{
				get
				{
					var prefix = this._useHttps ? "https://" : "http://";
					var url = prefix + "gravatar.com/avatar/" + Encode(Encoding.UTF8);

					if (this._appendFileType)
						url += ".jpg";

					url += BuildUrlParams();

					return url;
				}
			}

			/// <summary>
			/// Sets the size of the Gravatar.
			/// </summary>
			/// <param name="size">Size in pixels between 1 and 512.</param>
			public GravatarGenerator Size(int size)
			{
				if (size < 0 || size > 512)
					throw new ArgumentOutOfRangeException("size", "Image size must be between 1 and 512");

				_size = size;
				return this;
			}

			/// <summary>
			/// A default image to fall back to.
			/// </summary>
			/// <param name="defaultImage">A url to use as a default image.</param>
			public GravatarGenerator DefaultImage(string defaultImage)
			{
				_defaultImage = defaultImage;
				return this;
			}

			/// <summary>
			/// How to generate a default image if no gravatar exists for the email. See <see cref="Gravatar.DefaultGravatar"/> for details.
			/// </summary>
			/// <param name="defaultImage">What type of default image to generate.</param>
			public GravatarGenerator DefaultImage(DefaultGravatar defaultImage)
			{
				_defaultDisplay = defaultImage;
				return this;
			}

			/// <summary>
			/// Image rating to display for. See <see cref="Gravatar.GravatarRating"/> for details.
			/// </summary>
			/// <param name="rating">The rating to filter Gravatars for.</param>
			public GravatarGenerator Rating(GravatarRating rating)
			{
				_displayRating = rating;
				return this;
			}

			/// <summary>
			/// Wether to append a file ending to the url (.jpg).
			/// </summary>
			public GravatarGenerator AppendFileType()
			{
				_appendFileType = true;
				return this;
			}

			/// <summary>
			/// Force the default image to display.
			/// </summary>
			public GravatarGenerator ForceDefaultImage()
			{
				_forceDefaultImage = true;
				return this;
			}

			private string Encode(Encoding encoding)
			{
				var x = new System.Security.Cryptography.MD5CryptoServiceProvider();
				var hash = encoding.GetBytes(_email);
				hash = x.ComputeHash(hash);

				var sb = new StringBuilder();
				foreach (var t in hash)
				{
					sb.Append(t.ToString("x2"));
				}

				return sb.ToString();
			}

			private string GetRatingParam()
			{
				switch (this._displayRating)
				{
					case GravatarRating.G: return "r=g";
					case GravatarRating.PG: return "r=pg";
					case GravatarRating.R: return "r=r";
					case GravatarRating.X: return "r=x";
					default: return null;
				}
			}

			private string GetDefaultImageParam()
			{
				if (!string.IsNullOrWhiteSpace(this._defaultImage))
					return "d=" + HttpUtility.UrlEncode(this._defaultImage);

				switch (this._defaultDisplay)
				{
					case DefaultGravatar.IdentIcon: return "d=identicon";
					case DefaultGravatar.MonsterId: return "d=monsterid";
					case DefaultGravatar.MysteryMan: return "d=mm";
					case DefaultGravatar.None: return "d=404";
					case DefaultGravatar.Retro: return "d=retro";
					case DefaultGravatar.Wavatar: return "d=wavatar";
					default: return null;
				}
			}

			private string BuildUrlParams()
			{
				if (this._size < 0 || this._size > 512)
					throw new ArgumentOutOfRangeException("Size", "Image size must be between 1 and 512");

				var defaultImageParam = GetDefaultImageParam();
				var ratingParam = GetRatingParam();

				var urlParams = new List<string>();
				if (this._size > 0)
					urlParams.Add("s=" + this._size);
				if (!string.IsNullOrWhiteSpace(ratingParam))
					urlParams.Add(ratingParam);
				if (!string.IsNullOrWhiteSpace(defaultImageParam))
					urlParams.Add(defaultImageParam);
				if (this._forceDefaultImage)
					urlParams.Add("f=y");

				if (urlParams.Count == 0)
					return "";

				var paramString = "?";

				for (var i = 0; i < urlParams.Count; ++i)
				{
					paramString += urlParams[i];
					if (i < urlParams.Count - 1)
						paramString += "&";
				}

				return paramString;
			}
		}

		public static class GravatarURL
		{
			/// <summary>
			/// Gets a <see cref="Gravatar.GravatarGenerator"/> object.
			/// </summary>
			/// <param name="email">Email to generate Gravatar for.</param>
			/// <returns>A GravatarGenerator object.</returns>
			public static GravatarGenerator Generator(string email)
			{
				return new GravatarGenerator(email, false);
			}

			/// <summary>
			/// Gets a <see cref="Gravatar.GravatarGenerator"/> object.
			/// </summary>
			/// <param name="email">Email to generate Gravatar for.</param>
			/// <param name="size">The size in pixels, between 1 and 512.</param>
			/// <returns>A GravatarGenerator object</returns>
			public static GravatarGenerator Generator(string email, int size)
			{
				return new GravatarGenerator(email, false).Size(size);
			}

			/// <summary>
			/// Gets a Gravatar Url as string.
			/// </summary>
			/// <param name="email">Email to generate Gravatar for.</param>
			/// <param name="size">The size in pixels, between 1 and 512.</param>
			/// <returns>A Gravatar Url</returns>
			public static string Generate(string email, int size)
			{
				var gravatar = new GravatarGenerator(email, false).Size(size);
				return gravatar.Url;
			}

			/// <summary>
			/// Gets a Gravatar Url as string.
			/// </summary>
			/// <param name="email">Email to generate Gravatar for.</param>
			/// <param name="size">The size in pixels, between 1 and 512.</param>
			/// <param name="defaultImage">A default Gravatar generation policy. See <see cref="Gravatar.DefaultGravatar"/> for details.</param>
			/// <returns>A Gravatar Url</returns>
			public static string Generate(string email, int size, DefaultGravatar defaultImage)
			{
				var gravatar = new GravatarGenerator(email, false)
					.Size(size)
					.DefaultImage(defaultImage);
				return gravatar.Url;
			}

			/// <summary>
			/// Gets a Gravatar Url as string.
			/// </summary>
			/// <param name="email">Email to generate Gravatar for.</param>
			/// <param name="size">The size in pixels, between 1 and 512.</param>
			/// <param name="defaultImage">An Url to a default image to use if no Gravatar exists.</param>
			/// <returns>A Gravatar Url</returns>
			public static string Generate(string email, int size, string defaultImage)
			{
				var gravatar = new GravatarGenerator(email, false)
					.Size(size)
					.DefaultImage(defaultImage);
				return gravatar.Url;
			}

			/// <summary>
			/// Gets a Gravatar Url as string.
			/// </summary>
			/// <param name="email">Email to generate Gravatar for.</param>
			/// <param name="size">The size in pixels, between 1 and 512.</param>
			/// <param name="defaultImage">A default Gravatar generation policy. See <see cref="Gravatar.DefaultGravatar"/> for details.</param>
			/// <param name="rating">Image rating to display for. See <see cref="Gravatar.GravatarRating"/> for details.</param>
			/// <returns>A Gravatar Url</returns>
			public static string Generate(string email, int size, DefaultGravatar defaultImage, GravatarRating rating)
			{
				var gravatar = new GravatarGenerator(email, false)
					.Size(size)
					.Rating(rating)
					.DefaultImage(defaultImage);
				return gravatar.Url;
			}

			/// <summary>
			/// Gets a Gravatar Url as string.
			/// </summary>
			/// <param name="email">Email to generate Gravatar for.</param>
			/// <param name="size">The size in pixels, between 1 and 512.</param>
			/// <param name="defaultImage">An Url to a default image to use if no Gravatar exists.</param>
			/// <param name="rating">Image rating to display for. See <see cref="Gravatar.GravatarRating"/> for details.</param>
			/// <returns>A Gravatar Url</returns>
			public static string Generate(string email, int size, string defaultImage, GravatarRating rating)
			{
				var gravatar = new GravatarGenerator(email, false)
					.Size(size)
					.Rating(rating)
					.DefaultImage(defaultImage);
				return gravatar.Url;
			}
		}
	}
#endif
	#endregion
}