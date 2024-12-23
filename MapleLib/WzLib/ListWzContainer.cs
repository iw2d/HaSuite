﻿using System.Collections.Generic;
using System.IO;
using Castle.Components.DictionaryAdapter.Xml;
using MapleLib.WzLib.Util;
using MapleLib.WzLib.WzProperties;

namespace MapleLib.WzLib {
	public interface ListWzContainer {
		bool LoadListWz(string file);

		bool ListWzContains(string wzName, string wzEntry);

		bool IsInsideListWz(string fullPath);
	}

	public class ListWzContainerImpl {
		public static bool LoadListWz(List<string> ListWzEntries, byte[] WzIv, byte[] UserKey, string file) {
			if (!File.Exists(file)) return false;
			ListWzEntries.Clear();
			ListWzEntries.AddRange(ListFileParser.ParseListFile(file, WzIv, UserKey));
			return true;
		}

		public static bool ListWzContains(List<string> ListWzEntries, string wzName, string wzEntry) {
			wzEntry = wzEntry.ToLower().Replace(".wz/", "/");
			if (string.IsNullOrEmpty(wzName)) return ListWzEntries.Contains(wzEntry);
			// Strips anything that isn't the base wz name
			// Since I think.... those aren't needed
			wzName = WzFileManager.CleanWzName(wzName);
			if (!wzEntry.StartsWith(wzName)) {
				return ListWzEntries.Contains(wzName + "/" + wzEntry);
			}

			// Fixes things like "mob_test/0100100.img" to "mob/0100100.img"
			var index = wzEntry.IndexOf('/');

			if (index - 1 != wzName.Length) {
				wzEntry = wzName + wzEntry.Substring(index);
			}

			return ListWzEntries.Contains(wzEntry);
		}

		public static bool IsInsideListWz(List<string> ListWzEntries, string fullPath) {
			fullPath = fullPath.ToLower().Replace(".wz", "");
			foreach (string entry in ListWzEntries) {
				if (fullPath.StartsWith(entry)) {
					return true;
				}
			}
			return false;
		}


		public static void MarkListWzProperty(WzImage image) {
			var wzFile = image?.WzFileParent;
			if (wzFile == null) return;
			MarkListWzProperty(image, wzFile);
		}

		public static void MarkListWzProperty(WzImage image, WzFile wzFile, string overrideFullPath = null) {
			MarkListWzProperty(image, wzFile.ListWzEntries, wzFile.Name, overrideFullPath);
		}

		public static void MarkListWzProperty(WzImage image, List<string> listWzEntries, string wzName, string overrideFullPath = null) {
			var listWz = ListWzContains(listWzEntries, wzName, overrideFullPath ?? image.FullPath);
			foreach (var prop in image.WzProperties) {
				MarkListWzProperty(prop, listWz);
			}
		}

		public static void MarkListWzProperty(WzImage image, bool listWz) {
			foreach (var prop in image.WzProperties) {
				MarkListWzProperty(prop, listWz);
			}
		}

		public static void MarkListWzProperty(WzImageProperty parent, bool listWz, WzMutableKey wzKey = null) {
			if (parent.WzProperties == null) return;
			foreach (var prop in parent.WzProperties) {
				if (prop is WzCanvasProperty canvas) {
					var pngProp = canvas.PngProperty;
					var key = parent.ParentImage?.wzKey ?? wzKey;
					pngProp.ConvertCompressed(key, listWz ? key : null, !listWz);
				}

				MarkListWzProperty(prop, listWz);
			}
		}

		public static void RecursiveRemoveListWz(WzObject parentObj, WzObject obj) {
			if (obj is WzImage image) {
				// Decrypt any List.wz entries so that we paste them as decrypted
				// Otherwise we need to know the key to decrypt them on paste
				MarkListWzProperty(image, false);
			} else if (obj is WzImageProperty prop) {
				// No parent image so we need to grab it from source
				WzMutableKey wzKey = null;
				if (parentObj is WzImage img) {
					wzKey = img.wzKey;
				} else if (parentObj is WzImageProperty parentProp && parentProp.ParentImage != null) {
					wzKey = parentProp.ParentImage.wzKey;
				}

				MarkListWzProperty(prop, false, wzKey);
			} else if (obj is WzDirectory dir) {
				foreach (var img in dir.WzImages) {
					RecursiveRemoveListWz(dir, img);
				}

				foreach (var dirSub in dir.WzDirectories) {
					RecursiveRemoveListWz(dir, dirSub);
				}
			}
		}

		public static void ConvertKey(WzImage image) {
			foreach (var prop in image.WzProperties) {
				ConvertKey(prop);
			}
		}
		
		public static void ConvertKey(WzImageProperty parent, WzMutableKey decWzKey = null, WzMutableKey encWzKey = null) {
			if (parent.WzProperties == null) return;
			foreach (var prop in parent.WzProperties) {
				if (prop is WzCanvasProperty canvas) {
					var pngProp = canvas.PngProperty;
					var key = parent.ParentImage?.wzKey ?? decWzKey;
					var listWz = canvas.PngProperty.CheckListWzUsed();
					if (listWz) {
						pngProp.ConvertCompressed(key, listWz ? encWzKey : null, false);
					}
				}

				ConvertKey(prop, decWzKey, encWzKey);
			}
		}
	}
}