﻿/* Copyright (C) 2015 haha01haha01

* This Source Code Form is subject to the terms of the Mozilla Public
* License, v. 2.0. If a copy of the MPL was not distributed with this
* file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Windows;
using System.Windows.Forms;
using HaCreator.GUI.InstanceEditor;
using HaCreator.MapEditor;
using HaCreator.Wz;
using HaSharedLibrary.Wz;
using MapleLib.WzLib.WzProperties;
using MessageBox = System.Windows.Forms.MessageBox;
using TabControl = System.Windows.Controls.TabControl;
using XNA = Microsoft.Xna.Framework;

namespace HaCreator.GUI {
	public partial class New : Form {
		private readonly MultiBoard multiBoard;
		private readonly TabControl Tabs;
		private readonly RoutedEventHandler[] rightClickHandler;

		public New(MultiBoard board, TabControl Tabs,
			RoutedEventHandler[] rightClickHandler) {
			InitializeComponent();
			multiBoard = board;
			this.Tabs = Tabs;
			this.rightClickHandler = rightClickHandler;
		}

		/// <summary>
		/// On Load
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void New_Load(object sender, EventArgs e) {
			newWidth.Text = ApplicationSettings.LastMapSize.Width.ToString();
			newHeight.Text = ApplicationSettings.LastMapSize.Height.ToString();
		}

		#region Create new

		/// <summary>
		/// Creates a new default map to work from
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void newButton_Click(object sender, EventArgs e) {
			var w = int.Parse(newWidth.Text);
			var h = int.Parse(newHeight.Text);

			MapLoader.CreateMap(CreateReason.New, "", "<Untitled>", -1, "", MapLoader.CreateStandardMapMenu(rightClickHandler),
				new XNA.Point(w, h), new XNA.Point(w / 2, h / 2), Tabs, multiBoard);
			DialogResult = DialogResult.OK;
			Close();
		}

		#endregion

		#region Clone map

		/// <summary>
		/// Select a map
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button_SelectCloneMap_Click(object sender, EventArgs e) {
			var selector = new LoadMapSelector(numericUpDown1);
			selector.ShowDialog();
		}

		/// <summary>
		/// On map id selection changed
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void numericUpDown1_ValueChanged(object sender, EventArgs e) {
			if (numericUpDown1.Value != -1) {
				buttonCreateFrmClone.Enabled = true; // enable the button after selecting a map
			}
		}


		/// <summary>
		/// Button on create map
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void buttonCreateFrmClone_Click(object sender, EventArgs e) {
			if (numericUpDown1.Value == -1) {
				return;
			}

			var mapId = (int) numericUpDown1.Value;
			var mapId_str = mapId.ToString();

			var mapImage = WzInfoTools.FindMapImage(mapId_str, Program.WzManager);
			if (mapImage == null) {
				MessageBox.Show("Map is null.");
				return;
			}

			var strMapProp = WzInfoTools.GetMapStringProp(mapId, Program.WzManager);
			var cloneMapName = WzInfoTools.GetMapName(strMapProp);
			var cloneStreetName = WzInfoTools.GetMapStreetName(strMapProp);
			var cloneCategoryName = WzInfoTools.GetMapCategoryName(strMapProp);

			MapLoader.CreateMapFromImage(CreateReason.Clone, -1 /*mapid*/, mapImage.DeepClone(), cloneMapName, cloneStreetName,
				cloneCategoryName, (WzSubProperty) strMapProp.DeepClone(), Tabs, multiBoard, rightClickHandler);

			Close();
		}

		#endregion

		#region Misc

		private void New_KeyDown(object sender, KeyEventArgs e) {
			if (e.KeyCode == Keys.Escape) {
				Close();
			} else if (e.KeyCode == Keys.Enter) {
				newButton_Click(null, null);
			}
		}

		#endregion
	}
}