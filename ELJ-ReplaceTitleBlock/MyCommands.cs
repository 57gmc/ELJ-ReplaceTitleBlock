// (C) Copyright 2014 by TID 
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose with fee is hereby granted, 
// provided that the above copyright notice appears in all copies and 
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting 
// documentation.
//


using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using acApp = Autodesk.AutoCAD.ApplicationServices.Application;
using acWin = Autodesk.AutoCAD.Windows;
using TID_.Utilities;


// This line is not mandatory, but improves loading performance
[assembly: CommandClass(typeof(TID_.TID_Commands))]

namespace TID_
{
    /// <summary>
    /// This class is instantiated by AutoCAD for each document when
    /// a command is called by the user the first time in the context
    /// of a given document. In other words, non static data in this class
    /// is implicitly per-document!
    /// </summary>
    public class TID_Commands
    {

        ////  CommandName
        ///// <summary>
        ///// Use this section to provide a description of the method for xml documentation. 
        ///// </summary>
        //[CommandMethod("TID", "CommandNameGlobal", "CommandNameLocalized", CommandFlags.Modal)]
        //public void MethodName()
        //{

        //}

        //  TitleBlockChange
        /// <summary>
        /// TitleBlockChange is a command that changes the Title Block on the active drawing and 
        /// it tries to copy all of the attributes from the old Title Block to the new one.
        /// </summary>
        /// <remarks>Attributes common to both the old block and the new one will be retained. 
        /// All others will be empty in the new block.</remarks>
        [CommandMethod("TID", "TitleBlockChange", CommandFlags.Modal)]
        public void TitleBlockChange()
        {
            // Ask user For old block name
            PromptEntityOptions PEO = new PromptEntityOptions("Select title block to replace: ");
            PEO.SetRejectMessage("You need to select a block: ");
            PEO.AddAllowedClass(typeof(BlockReference), true);
            PEO.AllowNone = false;

            PromptEntityResult PER = Active.Editor.GetEntity(PEO);
            try
            {
                using (Transaction trans = Active.Database.TransactionManager.StartTransaction())
                {
                    BlockReference oldBlk = (BlockReference)trans.GetObject(PER.ObjectId, OpenMode.ForRead);

                    //Display Dialog Box Asking User to select New Title Block
                    acWin.OpenFileDialog ofd = new acWin.OpenFileDialog("Select a TitleBlock to import.", "", "dwg", "ReplaceTitleBlock",acWin.OpenFileDialog.OpenFileDialogFlags.SearchPath);
                    System.Windows.Forms.DialogResult dr = ofd.ShowDialog();
                     
                    if (dr != DialogResult.Cancel)
                    {
                         string sTitleBlock = sTitleBlock = ofd.Filename;

                        // Replace the old one.
                        // In order to get the atts to be equal to the new def, we need to delete
                        // current ref and insert new one, then update its atts.
                        oldBlk.UpgradeOpen();
                        oldBlk.Redefine(trans: trans, path: sTitleBlock, flag: Blocks.RedefineAction.PreserveAttValues, ""); 
                        trans.Commit();
                    }
                };

            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                //Throw Exception or ignore if user cancelled command.
                if (ex.ErrorStatus != ErrorStatus.NullObjectId)
                {
                    MessageBox.Show("Error Occured in TitleBlockInsert command. " + ex.Message + ", " + ex.HelpLink, "TitleBlockInsert Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };

            }


        }
  
    }

}


