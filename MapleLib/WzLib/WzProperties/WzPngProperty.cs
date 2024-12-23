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

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MapleLib.Helpers;
using MapleLib.WzLib.Util;

namespace MapleLib.WzLib.WzProperties {
	/// <summary>
	/// A property that contains the information for a bitmap
	/// https://docs.microsoft.com/en-us/windows/win32/direct3d9/compressed-texture-resources
	/// https://code.google.com/archive/p/libsquish/
	/// https://github.com/svn2github/libsquish
	/// http://www.sjbrown.co.uk/2006/01/19/dxt-compression-techniques/
	/// https://en.wikipedia.org/wiki/S3_Texture_Compression
	/// </summary>
	public class WzPngProperty : WzImageProperty {
		#region Fields

		private int width, height, pixFormat, magLevel;
		internal byte[] compressedImageBytes;
		internal Bitmap png;

		internal WzObject parent;

		internal bool listWzUsed;

		internal WzBinaryReader wzReader;
		internal long offs;

		#endregion

		#region Inherited Members

		public override void SetValue(object value) {
			if (value is Bitmap bitmap) {
				SetImage(bitmap);
			} else {
				compressedImageBytes = (byte[]) value;
			}
		}

		public override WzImageProperty DeepClone() {
			var clone = new WzPngProperty();
			clone.width = width;
			clone.height = height;
			clone.pixFormat = pixFormat;
			clone.magLevel = magLevel;
			clone.listWzUsed = listWzUsed;
			clone.SetValue(GetCompressedBytes(false));
			return clone;
		}

		public override object WzValue => GetImage(false);

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
		public override WzPropertyType PropertyType => WzPropertyType.PNG;

		public override void WriteValue(WzBinaryWriter writer, bool insideListWz) {
			throw new NotImplementedException("Cannot write a PngProperty");
		}

		/// <summary>
		/// Disposes the object
		/// </summary>
		public override void Dispose() {
			compressedImageBytes = null;
			if (png != null) {
				png.Dispose();
				png = null;
			}

			//this.wzReader.Close(); // closes at WzFile
			wzReader = null;
		}

		#endregion

		#region Custom Members

		/// <summary>
		/// The width of the bitmap
		/// </summary>
		public int Width {
			get => width;
			set => width = value;
		}

		/// <summary>
		/// The height of the bitmap
		/// </summary>
		public int Height {
			get => height;
			set => height = value;
		}

		/// <summary>
		/// The format of the bitmap
		/// </summary>
		public int PixFormat {
			get => pixFormat;
			set => pixFormat = value;
		}

		public bool ConvertPixFormat(int newFormat) {
			if (pixFormat == newFormat) return false;
			var bmp = GetImage(false);
			pixFormat = newFormat;
			CompressPng(bmp);
			return true;
		}

		public int MagLevel {
			get => magLevel;
			set => magLevel = value;
		}

		/// <summary>
		/// Wz PNG format to System.Drawing.Imaging.PixelFormat
		/// </summary>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		private PixelFormat GetBitmapPixelFormat() {
			switch (pixFormat) {
				case 1:
				case 2:
				case 3:
					return PixelFormat.Format32bppArgb;
				case 257:
					return PixelFormat.Format16bppArgb1555;
				case 513:
				case 517:
					return PixelFormat.Format16bppRgb565;
				case 1026:
				case 2050:
					return PixelFormat.Format32bppArgb;
				default:
					throw new ArgumentException($"Unknown pixFormat {pixFormat}");
			}
		}


		public bool ListWzUsed {
			get => listWzUsed;
			set {
				if (value == listWzUsed) return;
				listWzUsed = value;
				// Probably safe to swap to ConvertCompressedBytes?
				CompressPng(GetImage(false));
			}
		}

		/// <summary>
		/// The actual bitmap
		/// </summary>
		public Bitmap PNG {
			set {
				png = value;
				CompressPng(png);
			}
		}

		public void SetImage(Bitmap png) {
			this.png = png;
			CompressPng(png);
		}

		public Bitmap GetImage(bool saveInMemory) {
			if (png == null) ParsePng(saveInMemory);
			return png;
		}

		/// <summary>
		/// Creates a blank WzPngProperty
		/// </summary>
		public WzPngProperty() {
			name = "PNG";
		}

		/// <summary>
		/// Creates a blank WzPngProperty 
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="parseNow"></param>
		internal WzPngProperty(WzBinaryReader reader, bool parseNow) {
			name = "PNG";
			// Read compressed bytes
			wzReader = reader;
			width = reader.ReadCompressedInt();
			height = reader.ReadCompressedInt();
			pixFormat = reader.ReadCompressedInt();
			magLevel = reader.ReadCompressedInt();
			reader.BaseStream.Position += 4;
			offs = reader.BaseStream.Position;
			if (parseNow) {
				GetCompressedBytes(true);
			} else {
				lock (reader) { // lock WzBinaryReader, allowing it to be loaded from multiple threads at once
					var len = reader.ReadInt32() - 1;
					reader.BaseStream.Position += 1;
					if (len <= 0) return;
					reader.BaseStream.Position += len;
				}
			}
		}

		#endregion

		#region Parsing Methods

		public byte[] GetCompressedBytes(bool saveInMemory) {
			if (compressedImageBytes != null) return compressedImageBytes;
			lock (wzReader) { // lock WzBinaryReader, allowing it to be loaded from multiple threads at once
				var pos = wzReader.BaseStream.Position;
				wzReader.BaseStream.Position = offs;
				var len = wzReader.ReadInt32() - 1;
				if (len <= 0) { // possibility an image written with the wrong wzIv 
					throw new Exception("The length of the image is negative. WzPngProperty. Wrong WzIV?");
				}

				wzReader.BaseStream.Position += 1;

				compressedImageBytes = wzReader.ReadBytes(len);
				wzReader.BaseStream.Position = pos;
			}

			if (saveInMemory) return compressedImageBytes;
			// we're removing the reference to compressedBytes, so a backup for the ret value is needed
			var returnBytes = compressedImageBytes;
			compressedImageBytes = null;
			return returnBytes;
		}

		/// <summary>
		/// Checks if the image was compressed by List.wz and saves the result to ListWzUsed
		/// Doesn't use cached value to determine the result
		/// Will always check the compressed buffer for the zlib header
		/// </summary>
		/// <param name="compressedBuffer"></param>
		/// <returns></returns>
		public bool CheckListWzUsed(byte[] compressedBuffer = null) {
			if (compressedBuffer == null) {
				compressedBuffer = GetCompressedBytes(false);
			}

			// If the first 2 bytes aren't a zlib header, then it's probably compressed by List.wz
			var header = (ushort) (compressedBuffer[0] | ((uint) compressedBuffer[1] << 8));
			listWzUsed = header != 0x9C78 && header != 0xDA78 && header != 0x0178 && header != 0x5E78;
			return listWzUsed;
		}

		internal byte[] Decompress(byte[] compressedBuffer, byte[] decompressedBuffer) {
			using (var reader = new BinaryReader(new MemoryStream(compressedBuffer))) {
				DeflateStream zlib;

				if (CheckListWzUsed(compressedBuffer)) {
					var dataStream = new MemoryStream();
					var endOfPng = compressedBuffer.Length;

					// Read image into zlib
					while (reader.BaseStream.Position < endOfPng) {
						var blockSize = reader.ReadInt32();
						// Old MapleLib versions will incorrectly write extra bytes, just ignore them
						if (blockSize == 0) break;
						for (var i = 0; i < blockSize; i++) {
							dataStream.WriteByte((byte) (reader.ReadByte() ^ ParentImage.wzKey[i]));
						}
					}

					dataStream.Position = 2; // ZLib header
					zlib = new DeflateStream(dataStream, CompressionMode.Decompress);
				} else {
					reader.ReadInt16(); // ZLib header
					zlib = new DeflateStream(reader.BaseStream, CompressionMode.Decompress);
				}

				using (zlib) {
					var totalRead = 0;
					while (totalRead < decompressedBuffer.Length) {
						var read = zlib.Read(decompressedBuffer.AsSpan(totalRead));
						if (read == 0) {
							break;
						}

						totalRead += read;
					}

					return decompressedBuffer;
				}
			}
		}

		internal byte[] Compress(byte[] decompressedBuffer) {
			return Compress(decompressedBuffer, ParentImage?.wzKey);
		}

		internal byte[] Compress(byte[] decompressedBuffer, WzMutableKey WzKey) {
			using (var zlibStream = new MemoryStream()) {
				byte[] header = {0x78, 0x9C}; // Should we save the original header?
				using (var zip = new DeflateStream(zlibStream, CompressionMode.Compress, true)) {
					zip.Write(decompressedBuffer, 0, decompressedBuffer.Length);
				}

				if (listWzUsed) {
					using (var stream = new MemoryStream()) {
						stream.Write(BitConverter.GetBytes(header.Length), 0, 4);
						for (var i = 0; i < header.Length; i++) {
							stream.WriteByte((byte) (header[i] ^ WzKey[i]));
						}

						var buffer = zlibStream.ToArray();
						// What's the buffer capacity? That might be worth replicating
						stream.Write(BitConverter.GetBytes(buffer.Length), 0, 4);
						for (var i = 0; i < buffer.Length; i++)
							stream.WriteByte((byte) (buffer[i] ^ WzKey[i]));
						return stream.ToArray();
					}
				}

				{
					zlibStream.Position = 0;
					var buffer = new byte[zlibStream.Length + 2];
					zlibStream.Read(buffer, 2, buffer.Length - 2);

					Buffer.BlockCopy(header, 0, buffer, 0, header.Length);
					return buffer;
				}
			}
		}

		internal byte[] GetRawImageArray() {
			switch (pixFormat) {
				case 1: {
					return new byte[width * height * 2];
				}
				case 2: {
					return new byte[width * height * 4];
				}
				case 3: {
					return new byte[width * height * 4];
				}
				case 257: {
					return new byte[width * height * 2];
				}
				case 513: {
					return new byte[width * height * 2];
				}
				case 517: {
					return new byte[width * height / 128];
				}
				case 1026: {
					return new byte[width * height * 4];
				}
				case 2050: {
					return new byte[width * height];
				}
				default:
					ErrorLogger.Log(ErrorLevel.MissingFeature,
						string.Format("Unknown PNG format {0} {1}", pixFormat, magLevel));
					return null;
			}
		}

		/// <summary>
		/// Reads the wz, decompresses the image, and returns the raw image bytes
		/// </summary>
		/// <returns></returns>
		public byte[] GetRawImage(bool saveInMemory) {
			var rawImageBytes = GetCompressedBytes(saveInMemory);

			var decBuf = GetRawImageArray();

			return Decompress(rawImageBytes, decBuf);
		}

		public void ConvertCompressed(WzMutableKey DecWzKey, WzMutableKey EncWzKey, bool removeListFormat = false) {
			var (buffer, listWz, modified) = GetConvertedCompressed(DecWzKey, EncWzKey, removeListFormat);
			if (!modified) return;
			listWzUsed = listWz;
			compressedImageBytes = buffer;
			var parentImg = ParentImage;
			if (parentImg == null) return;
			parentImg.Changed = true;
		}

		public (byte[], bool, bool) GetConvertedCompressed(WzMutableKey DecWzKey, WzMutableKey EncWzKey, bool removeListFormat = false) {
			var compressedBuffer = GetCompressedBytes(false);
			var orgListWzUsed = CheckListWzUsed(compressedBuffer);
			var decrypt = orgListWzUsed && DecWzKey != null;
			var encrypt = EncWzKey != null;

			var nowListWzUsed = encrypt && !removeListFormat;

			// Probably some other cases that should be here

			if (!orgListWzUsed) {
				// If List.wz wasn't used, and we aren't encrypting, return
				if (!encrypt) {
					return (compressedBuffer, orgListWzUsed, false);
				}
				// Otherwise, we are encrypting, so modify it with the new encryption
			}

			// If we're just going to decrypt & re-encrypt with the same key, return
			if (decrypt && encrypt && DecWzKey.Equals(EncWzKey)) {
				return (compressedBuffer, orgListWzUsed, false);
			}

			// If it's encrypted but we don't have a key to decrypt
			if (orgListWzUsed && !decrypt) {
				return (compressedBuffer, orgListWzUsed, false);
			}

			var addListWz = nowListWzUsed && !orgListWzUsed;
			if (addListWz) {
				using (var reader = new BinaryReader(new MemoryStream(compressedBuffer))) {
					var dataStream = new MemoryStream();
					var endOfPng = compressedBuffer.Length;
					var headerLength = 2;
					dataStream.Write(BitConverter.GetBytes(headerLength), 0, 4);
					for (var i = 0; i < headerLength; i++) {
						dataStream.WriteByte((byte) (reader.ReadByte() ^ EncWzKey[i]));
					}

					var length = (int) (endOfPng - reader.BaseStream.Position);
					dataStream.Write(BitConverter.GetBytes(length), 0, 4);
					for (var i = 0; i < length; i++) {
						dataStream.WriteByte((byte) (reader.ReadByte() ^ EncWzKey[i]));
					}

					return (dataStream.ToArray(), true, true);
				}
			}

			using (var reader = new BinaryReader(new MemoryStream(compressedBuffer))) {
				var dataStream = new MemoryStream();
				var endOfPng = compressedBuffer.Length;

				while (reader.BaseStream.Position < endOfPng) {
					var blockSize = reader.ReadInt32();
					if (blockSize == 0) break;
					if (!removeListFormat) {
						dataStream.Write(BitConverter.GetBytes(blockSize), 0, 4);
					}

					for (var i = 0; i < blockSize; i++) {
						var data = reader.ReadByte();
						if (decrypt) {
							data ^= DecWzKey[i];
						}

						if (nowListWzUsed) {
							data ^= EncWzKey[i];
						}

						dataStream.WriteByte(data);
					}
				}

				return (dataStream.ToArray(), nowListWzUsed, true);
			}
		}

		public void ParsePng(bool saveInMemory) {
			var rawBytes = GetRawImage(saveInMemory);
			if (rawBytes == null) {
				png = null;
				return;
			}

			try {
				var bitmapFormat = GetBitmapPixelFormat();

				var rect = new Rectangle(0, 0, width, height);
				var bmp = new Bitmap(width, height, bitmapFormat);
				var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bitmapFormat);

				switch (pixFormat) {
					case 0x1: {
						DecompressImage_PixelDataBgra4444(rawBytes, width, height, bmp, bmpData);
						break;
					}
					case 0x2: {
						Marshal.Copy(rawBytes, 0, bmpData.Scan0, rawBytes.Length);
						bmp.UnlockBits(bmpData);
						break;
					}
					case 0x101: {
						// http://forum.ragezone.com/f702/wz-png-format-decode-code-1114978/index2.html#post9053713
						// "Npc.wz\\2570101.img\\info\\illustration2\\face\\0"
						// 2570107 is a decent example. Used KMS 353
						CopyBmpDataWithStride(rawBytes, bmp.Width * 2, bmpData);

						bmp.UnlockBits(bmpData);
						break;
					}
					case 0x201: {
						// UI.wz/Logo.img/Wizet or Nexon in pre-bb versions
						Marshal.Copy(rawBytes, 0, bmpData.Scan0, rawBytes.Length);
						bmp.UnlockBits(bmpData);
						break;
					}
					case 517: {
						DecompressImage_PixelDataForm517(rawBytes, width, height, bmp, bmpData);
						break;
					}
					case 1026: {
						// Familiar_000.wz\9960688.img\attack\info\hit\0
						// Effect_004.wz\Direction17.img\effect\ark\noise\800\0\24
						// Effect_017.wz\EliteMobEff.img\eliteMonster\0\0
						DecompressImageDXT3(rawBytes, width, height, bmp, bmpData);
						break;
					}
					case 2050: {
						// Skill_022.wz/40002.img/skill/400021006/effect
						DecompressImageDXT5(rawBytes, width, height, bmp, bmpData);
						break;
					}
					default:
						ErrorLogger.Log(ErrorLevel.MissingFeature, $"Unknown PNG format {pixFormat} {magLevel}");
						break;
				}
				
				png = bmp;
			} catch (InvalidDataException) {
				png = null;
			}
		}

		#region Decoders

		/// <summary>
		/// For debugging: an example of this image may be found at "Effect.wz\\5skill.img\\character_delayed\\0"
		/// </summary>
		/// <param name="rawData"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="bmp"></param>
		/// <param name="bmpData"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public unsafe void DecompressImage_PixelDataBgra4444(byte[] rawData, int width, int height, Bitmap bmp,
			BitmapData bmpData) {
			var uncompressedSize = width * height * 2;
			var decoded = new byte[uncompressedSize * 2];

			// Declare a pointer to the first element of the rawData array
			// This allows us to directly access the memory of the rawData array
			// without having to access it through the array indexer, which is slower
			/*fixed (byte* pRawData = rawData) {
				// Declare a pointer to the first element of the decoded array
				fixed (byte* pDecoded = decoded) {
					// Iterate over the elements of the rawData array using the pointer
					for (var i = 0; i < uncompressedSize; i++) {
						var byteAtPosition = *(pRawData + i);

						var low = byteAtPosition & 0x0F;
						var b = (byte) (low | (low << 4));
						*(pDecoded + i * 2) = b;

						var high = byteAtPosition & 0xF0;
						var g = (byte) (high | (high >> 4));
						*(pDecoded + i * 2 + 1) = g;
					}
				}
			}*/
			for (var i = 0; i < uncompressedSize; i++) {
				var byteAtPosition = rawData[i];

				var low = byteAtPosition & 0x0F;
				var b = (byte) (low | (low << 4));
				decoded[i * 2] = b;

				var high = byteAtPosition & 0xF0;
				var g = (byte) (high | (high >> 4));
				decoded[i * 2 + 1] = g;
			}

			// Copy the decoded data to the bitmap using a pointer
			Marshal.Copy(decoded, 0, bmpData.Scan0, decoded.Length);
			bmp.UnlockBits(bmpData);
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DecompressImage_PixelDataForm517(byte[] rawData, int width, int height, Bitmap bmp,
			BitmapData bmpData) {
			var decoded = new byte[width * height * 2];

			var lineIndex = 0;
			for (int j0 = 0, j1 = height / 16; j0 < j1; j0++) {
				var dstIndex = lineIndex;
				for (int i0 = 0, i1 = width / 16; i0 < i1; i0++) {
					var idx = (i0 + j0 * i1) * 2;
					var b0 = rawData[idx];
					var b1 = rawData[idx + 1];
					for (var k = 0; k < 16; k++) {
						decoded[dstIndex++] = b0;
						decoded[dstIndex++] = b1;
					}
				}

				for (var k = 1; k < 16; k++) {
					Array.Copy(decoded, lineIndex, decoded, dstIndex, width * 2);
					dstIndex += width * 2;
				}

				lineIndex += width * 32;
			}

			Marshal.Copy(decoded, 0, bmpData.Scan0, decoded.Length);
			bmp.UnlockBits(bmpData);
		}

		/// <summary>
		/// DXT3
		/// </summary>
		/// <param name="rawData"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="bmp"></param>
		/// <param name="bmpData"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DecompressImageDXT3(byte[] rawData, int width, int height, Bitmap bmp, BitmapData bmpData) {
			var decoded = new byte[width * height * 4];

			if (SquishPNGWrapper.CheckAndLoadLibrary()) {
				SquishPNGWrapper.DecompressImage(decoded, width, height, rawData,
					(int) SquishPNGWrapper.FlagsEnum.kDxt3);
				rgbaToBgra(decoded);
			} else { // otherwise decode here directly, fallback.
				var colorTable = new Color[4];
				var colorIdxTable = new int[16];
				var alphaTable = new byte[16];

				for (var y = 0; y < height; y += 4)
				for (var x = 0; x < width; x += 4) {
					var off = x * 4 + y * width;
					ExpandAlphaTableDXT3(alphaTable, rawData, off);
					var u0 = BitConverter.ToUInt16(rawData, off + 8);
					var u1 = BitConverter.ToUInt16(rawData, off + 10);
					ExpandColorTable(colorTable, u0, u1);
					ExpandColorIndexTable(colorIdxTable, rawData, off + 12);

					for (var j = 0; j < 4; j++)
					for (var i = 0; i < 4; i++) {
						SetPixel(decoded,
							x + i,
							y + j,
							width,
							colorTable[colorIdxTable[j * 4 + i]],
							alphaTable[j * 4 + i]);
					}
				}
			}

			Marshal.Copy(decoded, 0, bmpData.Scan0, decoded.Length);
			bmp.UnlockBits(bmpData);
		}

		/// <summary>
		/// DXT5
		/// </summary>
		/// <param name="rawData"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <param name="bmp"></param>
		/// <param name="bmpData"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DecompressImageDXT5(byte[] rawData, int width, int height, Bitmap bmp, BitmapData bmpData) {
			var decoded = new byte[width * height * 4];
			if (SquishPNGWrapper.CheckAndLoadLibrary()) {
				SquishPNGWrapper.DecompressImage(decoded, width, height, rawData,
					(int) SquishPNGWrapper.FlagsEnum.kDxt5);
				rgbaToBgra(decoded);
			} else { // otherwise decode here directly, fallback
				var colorTable = new Color[4];
				var colorIdxTable = new int[16];
				var alphaTable = new byte[8];
				var alphaIdxTable = new int[16];
				for (var y = 0; y < height; y += 4)
				for (var x = 0; x < width; x += 4) {
					var off = x * 4 + y * width;
					ExpandAlphaTableDXT5(alphaTable, rawData[off + 0], rawData[off + 1]);
					ExpandAlphaIndexTableDXT5(alphaIdxTable, rawData, off + 2);
					var u0 = BitConverter.ToUInt16(rawData, off + 8);
					var u1 = BitConverter.ToUInt16(rawData, off + 10);
					ExpandColorTable(colorTable, u0, u1);
					ExpandColorIndexTable(colorIdxTable, rawData, off + 12);

					for (var j = 0; j < 4; j++)
					for (var i = 0; i < 4; i++) {
						SetPixel(decoded,
							x + i,
							y + j,
							width,
							colorTable[colorIdxTable[j * 4 + i]],
							alphaTable[alphaIdxTable[j * 4 + i]]);
					}
				}
			}

			Marshal.Copy(decoded, 0, bmpData.Scan0, decoded.Length);
			bmp.UnlockBits(bmpData);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SetPixel(byte[] pixelData, int x, int y, int width, Color color, byte alpha) {
			var offset = (y * width + x) * 4;
			pixelData[offset + 0] = color.B;
			pixelData[offset + 1] = color.G;
			pixelData[offset + 2] = color.R;
			pixelData[offset + 3] = alpha;
		}

		private static unsafe void rgbaToBgra(byte[] data) {
			fixed (byte* pData = data) {
				for (var i = 0; i < data.Length; i += 4) {
					var r = *(pData + i);
					var g = *(pData + i + 1);
					var b = *(pData + i + 2);
					*(pData + i) = b;
					*(pData + i + 1) = g;
					*(pData + i + 2) = r;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void CopyBmpDataWithStride(byte[] source, int stride, BitmapData bmpData) {
			if (bmpData.Stride == stride) {
				Marshal.Copy(source, 0, bmpData.Scan0, source.Length);
			} else {
				for (var y = 0; y < bmpData.Height; y++) {
					Marshal.Copy(source, stride * y, bmpData.Scan0 + bmpData.Stride * y, stride);
				}
			}
		}

		#endregion

		#region DXT1 Color

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Color RGB565ToColor(ushort val) {
			const int rgb565_mask_r = 0xf800;
			const int rgb565_mask_g = 0x07e0;
			const int rgb565_mask_b = 0x001f;

			var r = (val & rgb565_mask_r) >> 11;
			var g = (val & rgb565_mask_g) >> 5;
			var b = val & rgb565_mask_b;
			var c = Color.FromArgb(
				(r << 3) | (r >> 2),
				(g << 2) | (g >> 4),
				(b << 3) | (b >> 2));
			return c;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ExpandColorTable(Color[] color, ushort c0, ushort c1) {
			color[0] = RGB565ToColor(c0);
			color[1] = RGB565ToColor(c1);
			if (c0 > c1) {
				color[2] = Color.FromArgb(0xff, (color[0].R * 2 + color[1].R + 1) / 3,
					(color[0].G * 2 + color[1].G + 1) / 3, (color[0].B * 2 + color[1].B + 1) / 3);
				color[3] = Color.FromArgb(0xff, (color[0].R + color[1].R * 2 + 1) / 3,
					(color[0].G + color[1].G * 2 + 1) / 3, (color[0].B + color[1].B * 2 + 1) / 3);
			} else {
				color[2] = Color.FromArgb(0xff, (color[0].R + color[1].R) / 2, (color[0].G + color[1].G) / 2,
					(color[0].B + color[1].B) / 2);
				color[3] = Color.FromArgb(0xff, Color.Black);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ExpandColorIndexTable(int[] colorIndex, byte[] rawData, int offset) {
			for (var i = 0; i < 16; i += 4, offset++) {
				colorIndex[i + 0] = rawData[offset] & 0x03;
				colorIndex[i + 1] = (rawData[offset] & 0x0c) >> 2;
				colorIndex[i + 2] = (rawData[offset] & 0x30) >> 4;
				colorIndex[i + 3] = (rawData[offset] & 0xc0) >> 6;
			}
		}

		#endregion

		#region DXT3/DXT5 Alpha

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ExpandAlphaTableDXT3(byte[] alpha, byte[] rawData, int offset) {
			for (var i = 0; i < 16; i += 2, offset++) {
				alpha[i + 0] = (byte) (rawData[offset] & 0x0f);
				alpha[i + 1] = (byte) ((rawData[offset] & 0xf0) >> 4);
			}

			for (var i = 0; i < 16; i++) alpha[i] = (byte) (alpha[i] | (alpha[i] << 4));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ExpandAlphaTableDXT5(byte[] alpha, byte a0, byte a1) {
			// get the two alpha values
			alpha[0] = a0;
			alpha[1] = a1;

			// compare the values to build the codebook
			if (a0 > a1) {
				for (var i = 2; i < 8; i++) // // use 7-alpha codebook
					alpha[i] = (byte) (((8 - i) * a0 + (i - 1) * a1 + 3) / 7);
			} else {
				for (var i = 2; i < 6; i++) // // use 5-alpha codebook
					alpha[i] = (byte) (((6 - i) * a0 + (i - 1) * a1 + 2) / 5);

				alpha[6] = 0;
				alpha[7] = 255;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ExpandAlphaIndexTableDXT5(int[] alphaIndex, byte[] rawData, int offset) {
			// write out the indexed codebook values
			for (var i = 0; i < 16; i += 8, offset += 3) {
				var flags = rawData[offset]
				            | (rawData[offset + 1] << 8)
				            | (rawData[offset + 2] << 16);

				// unpack 8 3-bit values from it
				for (var j = 0; j < 8; j++) {
					var mask = 0x07 << (3 * j);
					alphaIndex[i + j] = (flags & mask) >> (3 * j);
				}
			}
		}

		#endregion

		internal void CompressPng(Bitmap bmp) {
			if (pixFormat == 0) throw new Exception($"Unknown pixFormat {pixFormat}");

			width = bmp.Width;
			height = bmp.Height;

			var buf = GetRawImageArray();
			var rect = new Rectangle(0, 0, width, height);

			var bitmapFormat = GetBitmapPixelFormat();
			if (bmp.PixelFormat != bitmapFormat) {
				// This currently happens when you load a bitmap from file
				// Should probably fix it in the caller?
				Debug.WriteLine($"WzPngProperty.CompressPng: {bmp.PixelFormat} != {bitmapFormat}");
				png = bmp = bmp.Clone(rect, bitmapFormat);
			}

			switch (pixFormat) {
				case 0x1:
					CompressImage_PixelDataBgra4444(buf, bmp);
					break;
				case 0x2:
					WriteImage_PixelData(buf, bmp);
					break;
				case 0x101: {
					var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bitmapFormat);
					CopyFromBmpDataWithStride(buf, bmp.Width * 2, bmpData);
					bmp.UnlockBits(bmpData);
					break;
				}
				case 0x201: {
					var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bitmapFormat);
					Marshal.Copy(bmpData.Scan0, buf, 0, buf.Length);
					bmp.UnlockBits(bmpData);
					break;
				}
				case 0x402: {
					var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bitmapFormat);
					CompressImageDXT3(buf, bmpData);
					bmp.UnlockBits(bmpData);
					break;
				}
				case 0x802: {
					var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, bitmapFormat);
					CompressImageDXT5(buf, bmpData);
					bmp.UnlockBits(bmpData);
					break;
				}
				default:
					ErrorLogger.Log(ErrorLevel.MissingFeature, $"Unknown PNG format {pixFormat} {magLevel}");
					return;
			}

			compressedImageBytes = Compress(buf);
		}

		#region Encoders

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CompressImage_PixelDataBgra4444(byte[] buf, Bitmap bmp) {
			unsafe {
				fixed (byte* pBuf = buf) {
					var pCurPixel = pBuf;
					for (var y = 0; y < height; y++)
					for (var x = 0; x < width; x++) {
						var curPixel = bmp.GetPixel(x, y);
						*pCurPixel = (byte) ((curPixel.B >> 4) | (curPixel.G & 0xF0));
						*(pCurPixel + 1) = (byte) ((curPixel.R >> 4) | (curPixel.A & 0xF0));
						pCurPixel += 2;
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void WriteImage_PixelData(byte[] buf, Bitmap bmp) {
			unsafe {
				fixed (byte* pBuf = buf) {
					var pCurPixel = pBuf;
					for (var i = 0; i < height; i++)
					for (var j = 0; j < width; j++) {
						var curPixel = bmp.GetPixel(j, i);
						*pCurPixel = curPixel.B;
						*(pCurPixel + 1) = curPixel.G;
						*(pCurPixel + 2) = curPixel.R;
						*(pCurPixel + 3) = curPixel.A;
						pCurPixel += 4;
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void CopyFromBmpDataWithStride(byte[] dest, int stride, BitmapData bmpData) {
			if (bmpData.Stride == stride) {
				Marshal.Copy(bmpData.Scan0, dest, 0, dest.Length);
			} else {
				for (var y = 0; y < bmpData.Height; y++) {
					Marshal.Copy(bmpData.Scan0 + bmpData.Stride * y, dest, stride * y, stride);
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CompressImageDXT3(byte[] buf, BitmapData bmpData) {
			if (SquishPNGWrapper.CheckAndLoadLibrary()) {
				var decoded = new byte[width * height * 4];
				Marshal.Copy(bmpData.Scan0, decoded, 0, decoded.Length);
				bgraToRgba(decoded);
				SquishPNGWrapper.CompressImage(decoded, width, height, buf,
					(int) SquishPNGWrapper.FlagsEnum.kDxt3);
			} else {
				throw new Exception("SquishPNGWrapper not loaded, cannot compress DXT3");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void CompressImageDXT5(byte[] buf, BitmapData bmpData) {
			if (SquishPNGWrapper.CheckAndLoadLibrary()) {
				var decoded = new byte[width * height * 4];
				Marshal.Copy(bmpData.Scan0, decoded, 0, decoded.Length);
				bgraToRgba(decoded);
				SquishPNGWrapper.CompressImage(decoded, width, height, buf,
					(int) SquishPNGWrapper.FlagsEnum.kDxt5);
			} else {
				throw new Exception("SquishPNGWrapper not loaded, cannot compress DXT5");
			}
		}

		private static unsafe void bgraToRgba(byte[] data) {
			fixed (byte* pData = data) {
				for (var i = 0; i < data.Length; i += 4) {
					var b = *(pData + i);
					var g = *(pData + i + 1);
					var r = *(pData + i + 2);
					*(pData + i) = r;
					*(pData + i + 1) = g;
					*(pData + i + 2) = b;
				}
			}
		}

		#endregion

		#endregion

		#region Cast Values

		public override Bitmap GetBitmap() {
			return GetImage(false);
		}

		#endregion

		public enum CanvasPixFormat {
			Unknown = 0x0,
			Argb4444 = 0x1,
			Argb8888 = 0x2,
			Argb1555 = 0x101,
			Rgb565 = 0x201,
			DXT3 = 0x402,
			DXT5 = 0x802
		}

		public bool IsArgb4444Compatible() {
			GetImage(false); // Load png if missing.

			for (var y = 0; y < height; y++)
			for (var x = 0; x < width; x++) {
				var curPixel = png.GetPixel(x, y);
				if (Math.Abs(curPixel.B / 17.0 % 1) > double.Epsilon * 100) return false;
				if (Math.Abs(curPixel.G / 17.0 % 1) > double.Epsilon * 100) return false;
				if (Math.Abs(curPixel.R / 17.0 % 1) > double.Epsilon * 100) return false;
				if (Math.Abs(curPixel.A / 17.0 % 1) > double.Epsilon * 100) return false;
			}

			return true;
		}
	}
}