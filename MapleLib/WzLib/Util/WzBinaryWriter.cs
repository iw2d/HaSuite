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

using System.Collections;
using System.IO;
using System.Linq;
using MapleLib.MapleCryptoLib;
using MapleLib.WzLib.WzStructure.Enums;

namespace MapleLib.WzLib.Util {
	/*
	   TODO : Maybe WzBinaryReader/Writer should read and contain the hash (this is probably what's going to happen)
	*/
	public class WzBinaryWriter : BinaryWriter {
		#region Properties

		public WzMutableKey WzKey { get; set; }
		public uint Hash { get; set; }
		public Hashtable StringCache { get; set; }
		public WzHeader Header { get; set; }
		public bool LeaveOpen { get; internal set; }

		#endregion

		#region Constructors

		public WzBinaryWriter(Stream output, byte[] WzIv, byte[] UserKey)
			: this(output, WzIv, UserKey, false) {
			Hash = 0;
		}

		public WzBinaryWriter(Stream output, byte[] WzIv, byte[] UserKey, uint Hash)
			: this(output, WzIv, UserKey, false) {
			this.Hash = Hash;
		}

		public WzBinaryWriter(Stream output, byte[] WzIv, byte[] UserKey, bool leaveOpen)
			: base(output) {
			WzKey = WzKeyGenerator.GenerateWzKey(WzIv, UserKey);
			StringCache = new Hashtable();
			LeaveOpen = leaveOpen;
		}

		#endregion

		#region Methods

		/// <summary>
		/// ?InternalSerializeString@@YAHPAGPAUIWzArchive@@EE@Z
		/// </summary>
		/// <param name="s"></param>
		/// <param name="withoutOffset">bExistID_0x73   0x73</param>
		/// <param name="withOffset">bNewID_0x1b  0x1B</param>
		public void WriteStringValue(string s, int withoutOffset, int withOffset, bool insideListWz = false) {
			// if length is > 4 and the string cache contains the string
			// writes the offset instead
			if (s.Length > 4 && StringCache.ContainsKey(s)) {
				Write((byte) withOffset);
				Write((int) StringCache[s]);
			} else {
				Write((byte) withoutOffset);
				var sOffset = (int) BaseStream.Position;
				Write(s, insideListWz);
				if (!StringCache.ContainsKey(s)) StringCache[s] = sOffset;
			}
		}

		/// <summary>
		/// Writes the Wz object value
		/// </summary>
		/// <param name="stringObjectValue"></param>
		/// <param name="type"></param>
		/// <param name="unk_GMS230"></param>
		/// <returns>true if the Wz object value is written as an offset in the Wz file, else if not</returns>
		public bool WriteWzObjectValue(string stringObjectValue, WzDirectoryType type, bool insideListWz = false) {
			var storeName = string.Format("{0}_{1}", (byte) type, stringObjectValue);

			// if length is > 4 and the string cache contains the string
			// writes the offset instead
			if (stringObjectValue.Length > 4 && StringCache.ContainsKey(storeName)) {
				Write((byte) WzDirectoryType.RetrieveStringFromOffset_2); // 2
				Write((int) StringCache[storeName]);

				return true;
			}

			var sOffset = (int) (BaseStream.Position - Header.FStart);
			Write((byte) type);
			Write(stringObjectValue, insideListWz);
			if (!StringCache.ContainsKey(storeName)) StringCache[storeName] = sOffset;

			return false;
		}

		public void Write(string value, bool insideListWz = false) {
			if (value.Length == 0) {
				Write((byte) 0);
			} else {
				var unicode = value.Any(c => c > sbyte.MaxValue);

				if (unicode) {
					ushort mask = 0xAAAA;

					if (value.Length >=
					    sbyte.MaxValue) // Bugfix - >= because if value.Length = MaxValue, MaxValue will be written and then treated as a long-length marker
					{
						Write(sbyte.MaxValue);
						Write(value.Length);
					} else {
						Write((sbyte) value.Length);
					}

					var i = 0;
					foreach (var character in value) {
						var encryptedChar = (ushort) character;
						if (insideListWz) {
							encryptedChar ^= (ushort)((WzKey[i * 2 + 1] << 8) + WzKey[i * 2]);
						}
						encryptedChar ^= mask;
						mask++;
						Write(encryptedChar);

						i++;
					}
				} else // ASCII
				{
					byte mask = 0xAA;

					if (value.Length >
					    sbyte.MaxValue) // Note - no need for >= here because of 2's complement (MinValue == -(MaxValue + 1))
					{
						Write(sbyte.MinValue);
						Write(value.Length);
					} else {
						Write((sbyte) -value.Length);
					}

					var i = 0;
					foreach (var c in value) {
						var encryptedChar = (byte) c;
						if (insideListWz) {
							encryptedChar ^= WzKey[i];
						}
						encryptedChar ^= mask;
						mask++;
						Write(encryptedChar);

						i++;
					}
				}
			}
		}

		public void Write(string value, int length) {
			for (var i = 0; i < length; i++) {
				if (i < value.Length) {
					Write(value[i]);
				} else {
					Write((byte) 0);
				}
			}
		}

		public char[] EncryptString(string stringToDecrypt) {
			var outputChars = new char[stringToDecrypt.Length];
			for (var i = 0; i < stringToDecrypt.Length; i++)
				outputChars[i] = (char) (stringToDecrypt[i] ^ (char) ((WzKey[i * 2 + 1] << 8) + WzKey[i * 2]));
			return outputChars;
		}

		public char[] EncryptNonUnicodeString(string stringToDecrypt) {
			var outputChars = new char[stringToDecrypt.Length];
			for (var i = 0; i < stringToDecrypt.Length; i++)
				outputChars[i] = (char) (stringToDecrypt[i] ^ WzKey[i]);
			return outputChars;
		}

		public void WriteNullTerminatedString(string value) {
			for (var i = 0; i < value.Length; i++) Write((byte) value[i]);

			Write((byte) 0);
		}

		public void WriteCompressedInt(int value) {
			if (value > sbyte.MaxValue || value <= sbyte.MinValue) {
				Write(sbyte.MinValue);
				Write(value);
			} else {
				Write((sbyte) value);
			}
		}

		public void WriteCompressedLong(long value) {
			if (value > sbyte.MaxValue || value <= sbyte.MinValue) {
				Write(sbyte.MinValue);
				Write(value);
			} else {
				Write((sbyte) value);
			}
		}

		public void WriteOffset(uint value) {
			var encOffset = (uint) BaseStream.Position;
			encOffset = (encOffset - Header.FStart) ^ 0xFFFFFFFF;
			encOffset *= Hash; // could this be removed? 
			encOffset -= MapleCryptoConstants.WZ_OffsetConstant;
			encOffset = RotateLeft(encOffset, (byte) (encOffset & 0x1F));
			var writeOffset = encOffset ^ (value - Header.FStart * 2);
			Write(writeOffset);
		}

		private uint RotateLeft(uint x, byte n) {
			return (x << n) | (x >> (32 - n));
		}

		private uint RotateRight(uint x, byte n) {
			return (x >> n) | (x << (32 - n));
		}

		public override void Close() {
			if (LeaveOpen) return;
			base.Close();
		}

		#endregion
	}
}