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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties {
	/// <summary>
	/// A property that contains several WzExtendedPropertys
	/// </summary>
	public class WzConvexProperty : WzExtended, IPropertyContainer {
		#region Fields

		internal List<WzImageProperty> properties = new List<WzImageProperty>();

		internal WzObject parent;
		//internal WzImage imgParent;

		#endregion

		#region Inherited Members

		public override void SetValue(object value) {
			throw new NotImplementedException();
		}

		public override WzImageProperty DeepClone() {
			var clone = new WzConvexProperty(name);
			foreach (var prop in properties) {
				clone.AddProperty(prop.DeepClone(), false);
			}

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
		public override WzPropertyType PropertyType => WzPropertyType.Convex;

		/// <summary>
		/// The properties contained in the property
		/// </summary>
		public override List<WzImageProperty> WzProperties =>
			properties; //properties.ConvertAll<IWzImageProperty>(new Converter<IExtended, IWzImageProperty>(delegate(IExtended source) { return (IWzImageProperty)source; }));

		/// <summary>
		/// Gets a wz property by it's name
		/// </summary>
		/// <param name="name">The name of the property</param>
		/// <returns>The wz property with the specified name</returns>
		public override WzImageProperty this[string name] {
			get {
				var nameLower = name.ToLower();
				foreach (var iwp in properties) {
					if (iwp.Name.ToLower() == nameLower) {
						return iwp;
					}
				}

				//throw new KeyNotFoundException("A wz property with the specified name was not found");
				return null;
			}
		}

		public WzImageProperty GetProperty(string name) {
			var nameLower = name.ToLower();
			foreach (var iwp in properties) {
				if (iwp.Name.ToLower() == nameLower) {
					return iwp;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets a wz property by a path name
		/// </summary>
		/// <param name="path">path to property</param>
		/// <returns>the wz property with the specified name</returns>
		public override WzImageProperty GetFromPath(string path) {
			var segments = path.Split(new char[1] {'/'}, StringSplitOptions.RemoveEmptyEntries);
			if (segments[0] == "..") return ((WzImageProperty) Parent)[path.Substring(name.IndexOf('/') + 1)];

			WzImageProperty ret = this;
			foreach (var segment in segments) {
				ret = ret.WzProperties.FirstOrDefault(iwp => iwp.Name == segment);
				if (ret == null) break;
			}

			return ret;
		}

		public override void WriteValue(WzBinaryWriter writer, bool insideListWz) {
			var extendedProps = new List<WzExtended>(properties.Count);
			foreach (var prop in properties) {
				if (prop is WzExtended extended) {
					extendedProps.Add(extended);
				}
			}

			writer.WriteStringValue("Shape2D#Convex2D", WzImage.WzImageHeaderByte_WithoutOffset,
				WzImage.WzImageHeaderByte_WithOffset, insideListWz);
			writer.WriteCompressedInt(extendedProps.Count);

			foreach (var imgProperty in properties) imgProperty.WriteValue(writer, insideListWz);
		}

		public override void ExportXml(StreamWriter writer, int level) {
			writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.OpenNamedTag("WzConvex", Name, true));
			DumpPropertyList(writer, level, WzProperties);
			writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.CloseTag("WzConvex"));
		}

		public override void Dispose() {
			name = null;
			foreach (var exProp in properties)
				exProp.Dispose();
			properties.Clear();
			properties = null;
		}

		#endregion

		#region Custom Members

		/// <summary>
		/// Creates a blank WzConvexProperty
		/// </summary>
		public WzConvexProperty() {
		}

		/// <summary>
		/// Creates a WzConvexProperty with the specified name
		/// </summary>
		/// <param name="name">The name of the property</param>
		public WzConvexProperty(string name) {
			this.name = name;
		}

		/// <summary>
		/// Adds a WzExtendedProperty to the list of properties
		/// </summary>
		/// <param name="prop">The property to add</param>
		public void AddProperty(WzImageProperty prop, bool checkListWz = true) {
			if (!(prop is WzExtended extended)) {
				throw new Exception("Property is not IExtended");
			}

			extended.Parent = this;
			properties.Add(extended);

			if (!checkListWz) {
				return;
			}

			var image = ParentImage;
			if (image == null || !image.Parsed) return;
			var wzFile = WzFileParent;
			if (wzFile == null) return;
			ListWzContainerImpl.MarkListWzProperty(image, wzFile);
		}

		public void AddProperties(List<WzImageProperty> properties) {
			foreach (var property in properties)
				AddProperty(property);
		}

		public void RemoveProperty(WzImageProperty prop) {
			prop.Parent = null;
			properties.Remove(prop);
		}

		public void ClearProperties() {
			foreach (var prop in properties) prop.Parent = null;
			properties.Clear();
		}

		#endregion
	}
}