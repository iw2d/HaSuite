﻿/*  MapleLib - A general-purpose MapleStory library
 * Copyright (C) 2009, 2010, 2015 Snow and haha01haha01

 * This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

 * This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.*/

using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties {
	/// <summary>
	/// A wz property which has a value which is a ushort
	/// </summary>
	public class WzShortProperty : WzImageProperty {
		#region Fields

		internal short val;

		internal WzObject parent;
		//internal WzImage imgParent;

		#endregion

		#region Inherited Members

		public override void SetValue(object value) {
			val = (short) value;
		}

		public override WzImageProperty DeepClone() {
			var clone = new WzShortProperty(name, val);
			return clone;
		}

		public override object WzValue => Value;

		/// <summary>
		/// The parent of the object
		/// </summary>
		public override WzObject Parent {
			get => parent;
			internal set => parent = value;
		}

		/// <summary>
		/// The WzPropertyType of the property
		/// </summary>
		public override WzPropertyType PropertyType => WzPropertyType.Short;

		public override void WriteValue(WzBinaryWriter writer, bool insideListWz) {
			writer.Write((byte) 2);
			writer.Write(Value);
		}

		public override void ExportXml(StreamWriter writer, int level) {
			writer.WriteLine(XmlUtil.Indentation(level) +
			                 XmlUtil.EmptyNamedValuePair("WzUnsignedShort", Name, Value.ToString()));
		}

		/// <summary>
		/// Disposes the object
		/// </summary>
		public override void Dispose() {
			name = null;
		}

		#endregion

		#region Custom Members

		/// <summary>
		/// The value of the property
		/// </summary>
		public short Value {
			get => val;
			set => val = value;
		}

		/// <summary>
		/// Creates a blank WzUnsignedShortProperty
		/// </summary>
		public WzShortProperty() {
		}

		/// <summary>
		/// Creates a WzUnsignedShortProperty with the specified name
		/// </summary>
		/// <param name="name">The name of the property</param>
		public WzShortProperty(string name) {
			this.name = name;
		}

		/// <summary>
		/// Creates a WzUnsignedShortProperty with the specified name and value
		/// </summary>
		/// <param name="name">The name of the property</param>
		/// <param name="value">The value of the property</param>
		public WzShortProperty(string name, short value) {
			this.name = name;
			val = value;
		}

		#endregion

		#region Cast Values

		public override float GetFloat() {
			return val;
		}

		public override double GetDouble() {
			return val;
		}

		public override int GetInt() {
			return val;
		}

		public override short GetShort() {
			return val;
		}

		public override long GetLong() {
			return val;
		}

		public override string ToString() {
			return val.ToString();
		}

		#endregion
	}
}