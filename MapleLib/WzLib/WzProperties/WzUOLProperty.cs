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

//uncomment to enable automatic UOL resolving, comment to disable it

#define UOLRES

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using MapleLib.Helpers;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties {
	/// <summary>
	/// A property that's value is a string
	/// </summary>
	public class WzUOLProperty : WzExtended {
		#region Fields

		internal string val;

		internal WzObject parent;

		//internal WzImage imgParent;
		internal WzObject linkVal;

		#endregion

		#region Inherited Members

		public override void SetValue(object value) {
			val = (string) value;
		}

		public override WzImageProperty DeepClone() {
			var clone = new WzUOLProperty(name, val);
			clone.linkVal = null;
			return clone;
		}

		public override object WzValue {
			get {
#if UOLRES
				return LinkValue;
#else
                return this;
#endif
			}
		}

		/// <summary>
		/// The parent of the object
		/// </summary>
		public override WzObject Parent {
			get => parent;
			internal set => parent = value;
		}

#if UOLRES
		public override List<WzImageProperty> WzProperties =>
			LinkValue is WzImageProperty ? ((WzImageProperty) LinkValue).WzProperties : null;


		public override WzImageProperty this[string name] =>
			LinkValue is WzImageProperty ? ((WzImageProperty) LinkValue)[name] :
			LinkValue is WzImage ? ((WzImage) LinkValue)[name] : null;

		public override WzImageProperty GetFromPath(string path) {
			return LinkValue is WzImageProperty ? ((WzImageProperty) LinkValue).GetFromPath(path) :
				LinkValue is WzImage ? ((WzImage) LinkValue).GetFromPath(path) : null;
		}
#endif

		/// <summary>
		/// The WzPropertyType of the property
		/// </summary>
		public override WzPropertyType PropertyType => WzPropertyType.UOL;

		public override void WriteValue(WzBinaryWriter writer, bool insideListWz) {
			writer.WriteStringValue("UOL", WzImage.WzImageHeaderByte_WithoutOffset,
				WzImage.WzImageHeaderByte_WithOffset, insideListWz);
			writer.Write((byte) 0);
			writer.WriteStringValue(Value, 0, 1, insideListWz);
		}

		public override void ExportXml(StreamWriter writer, int level) {
			writer.WriteLine(XmlUtil.Indentation(level) + XmlUtil.EmptyNamedValuePair("WzUOL", Name, Value));
		}

		/// <summary>
		/// Disposes the object
		/// </summary>
		public override void Dispose() {
			name = null;
			val = null;
		}

		#endregion

		#region Custom Members

		/// <summary>
		/// The value of the property
		/// </summary>
		public string Value {
			get => val;
			set => val = value;
		}

#if UOLRES
		public WzObject LinkValue {
			get {
				if (linkVal == null) {
					var paths = val.Split('/');
					linkVal = parent;
					var fullPath = parent.FullPath;

					foreach (var path in paths) {
						if (path == "..") {
							linkVal = linkVal.Parent;
						} else {
							if (linkVal is WzImageProperty property) {
								linkVal = property[path];
							} else if (linkVal is WzImage image) {
								linkVal = image[path];
							} else if (linkVal is WzDirectory directory) {
								if (path.EndsWith(".img")) {
									linkVal = directory[path];
								} else {
									linkVal = directory[path + ".img"];
								}
							} else {
								ErrorLogger.Log(ErrorLevel.Critical,
									"UOL got nexon'd at property: " + FullPath);
								return null;
							}
						}
					}
				}

				return linkVal;
			}
		}
#endif

		/// <summary>
		/// Creates a blank WzUOLProperty
		/// </summary>
		public WzUOLProperty() {
		}

		/// <summary>
		/// Creates a WzUOLProperty with the specified name
		/// </summary>
		/// <param name="name">The name of the property</param>
		public WzUOLProperty(string name) {
			this.name = name;
		}

		/// <summary>
		/// Creates a WzUOLProperty with the specified name and value
		/// </summary>
		/// <param name="name">The name of the property</param>
		/// <param name="value">The value of the property</param>
		public WzUOLProperty(string name, string value) {
			this.name = name;
			val = value;
		}

		#endregion

		#region Cast Values

#if UOLRES
		public override int GetInt() {
			return LinkValue.GetInt();
		}

		public override short GetShort() {
			return LinkValue.GetShort();
		}

		public override long GetLong() {
			return LinkValue.GetLong();
		}

		public override float GetFloat() {
			return LinkValue.GetFloat();
		}

		public override double GetDouble() {
			return LinkValue.GetDouble();
		}

		public override string GetString() {
			return LinkValue.GetString();
		}

		public override Point GetPoint() {
			return LinkValue.GetPoint();
		}

		public override Bitmap GetBitmap() {
			return LinkValue.GetBitmap();
		}

		public override byte[] GetBytes() {
			return LinkValue.GetBytes();
		}
#else
        public override string GetString()
        {
            return val;
        }
#endif
		public override string ToString() {
			return val;
		}

		#endregion
	}
}