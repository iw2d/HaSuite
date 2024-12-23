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

using System;
using System.IO;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties {
	/// <summary>
	/// A property that's value is null
	/// </summary>
	public class WzNullProperty : WzImageProperty {
		#region Fields

		internal WzObject parent;
		//internal WzImage imgParent;

		#endregion

		#region Inherited Members

		public override void SetValue(object value) {
			throw new NotImplementedException();
		}

		public override WzImageProperty DeepClone() {
			var clone = new WzNullProperty(name);
			return clone;
		}

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
		public override WzPropertyType PropertyType => WzPropertyType.Null;

		/// <summary>
		/// The WzObjectType of the property
		/// </summary>
		public override WzObjectType ObjectType => WzObjectType.Property;

		public override void WriteValue(WzBinaryWriter writer, bool insideListWz) {
			writer.Write((byte) 0);
		}

		public override void ExportXml(StreamWriter writer, int level) {
			writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.EmptyNamedTag("WzNull", Name));
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
		/// Creates a blank WzNullProperty
		/// </summary>
		public WzNullProperty() {
		}

		/// <summary>
		/// Creates a WzNullProperty with the specified name
		/// </summary>
		/// <param name="propName">The name of the property</param>
		public WzNullProperty(string propName) {
			name = propName;
		}

		#endregion
	}
}