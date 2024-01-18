// (C) Copyright 2009 by TID 
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose with fee is hereby granted, 
// provided that the above copyright notice appears in all copies and 
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting 
// documentation.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Autodesk.AutoCAD;
using Autodesk.AutoCAD.ApplicationServices;
using acApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using acWin = Autodesk.AutoCAD.Windows;

namespace TID_.Utilities
{

   //  AttInfo
   /// <summary>
   /// Class for temporarily holding some basic Attribute properties. It is used for creating an AttributeCollection 
   /// from a block definition upon inserting a block.
   /// </summary>
   public class AttInfo
   {
      private Point3d _pos;
      private Point3d _aln;
      private bool _aligned;
      private string _text;
      private string _tag;

      /// <summary>
      /// Default constructor.
      /// </summary>
      /// <param name="pos">Point3D position of insertion point.</param>
      /// <param name="aln">Point3D position of Alignment point. Use same as pos if left aligned.</param>
      /// <param name="aligned">Boolean indicating whether or not this attribute uses a text alignment other than left.</param>
      /// <returns><c>AttInfo</c> object containing the stored properties.</returns>
      public AttInfo(Point3d pos, Point3d aln, bool aligned, string Tag, string TextString)
      {
         _pos = pos;
         _aln = aln;
         _aligned = aligned;
         _tag = Tag;
         _text = TextString;
      }

      /// <summary>
      /// Point3D position of insertion point.
      /// </summary>
      public Point3d Position
      {
         set { _pos = value; }
         get { return _pos; }
      }

      /// <summary>
      /// Point3D position of Alignment point.
      /// </summary>
      public Point3d Alignment
      {
         set { _aln = value; }
         get { return _aln; }
      }

      /// <summary>
      /// Boolean indicating whether or not this attribute uses a text alignment other than left.
      /// </summary>
      public bool IsAligned
      {
         set { _aligned = value; }
         get { return _aligned; }
      }

      /// <summary>
      /// StringTextString.
      /// </summary>
      public string Tag
      {
         set { _tag = value; }
         get { return _tag; }
      }

      /// <summary>
      /// StringTextString.
      /// </summary>
      public string TextString
      {
         set { _text = value; }
         get { return _text; }
      }
   } 

    //  Blocks
    /// <summary>
    /// Static class collection of block utilities.
    /// </summary>
    public static class Blocks
    {
       //  AppendAttribRefToBlockRef
        /// <summary>
        /// AppendAttribRefToBlockRef is a funtion that appends attributes to a newly inserted block.
        /// </summary>
        /// <param name="acadDB">The working database that the block was inserted into.</param>
        /// <param name="blockdefid">Objectid of the Block Definition</param>
        /// <param name="blockrefid">Objectid of the Inserted Block Reference</param>
        /// <returns>Bool that lets program know if the operation was successful or not.</returns>
        public static bool AppendAttribRefToBlockRef(Database acadDB, ObjectId blockdefid, ObjectId blockrefid) //, List<string> attribs)
        {
            Transaction m_Transaction = acadDB.TransactionManager.StartTransaction();
            try
            {
                BlockTableRecord blkdef = (BlockTableRecord)m_Transaction.GetObject(blockdefid, OpenMode.ForRead);
                //int i = 0;
                if (blkdef.HasAttributeDefinitions)
                {
                    BlockReference blkref = (BlockReference)m_Transaction.GetObject(blockrefid, OpenMode.ForWrite);
                    foreach (ObjectId id in blkdef)
                    {
                        DBObject ent = m_Transaction.GetObject(id, OpenMode.ForRead);
                        if (ent.GetType() == typeof(AttributeDefinition))
                        {
                            AttributeDefinition attdef = (AttributeDefinition)ent;
                            AttributeReference attref = new AttributeReference();
                            attref.SetAttributeFromBlock(attdef, blkref.BlockTransform);
                            if (attdef.Justify == AttachmentPoint.BaseLeft)
                                attref.Position = attdef.Position.TransformBy(blkref.BlockTransform);
                            else
                                attref.AlignmentPoint = attdef.AlignmentPoint.TransformBy(blkref.BlockTransform);
                            if (attref.IsMTextAttribute)
                                attref.UpdateMTextAttribute();
                            //if (i < attribs.Count)
                            //    attref.TextString = attribs[i++];
                            //else
                                attref.TextString = attdef.TextString;
                            blkref.AttributeCollection.AppendAttribute(attref);
                            m_Transaction.AddNewlyCreatedDBObject(attref, true);
                        }
                    }
                }
                m_Transaction.Commit();
                m_Transaction.Dispose();
                return true;
            }
            catch
            { }
            m_Transaction.Abort();
            m_Transaction.Dispose();
            return false;
        }

        //  BlockInfo
       /// <summary>
       /// Stucture for temporarily holding some basic Block insertion properties. It's properties have similar types
       /// as that of a BlockReference object.
       /// <list type="bullet"> 
       ///  <listheader><term>Property Name</term><description>Type</description></listheader>
       ///  <item><term>Attributes</term><description><cref>Autodesk.AutoCAD.DatabaseServices.AttributeCollection</cref></description></item>
       ///  <item><term>BlockUnit</term><description><cref>Autodesk.AutoCAD.DatabaseServices.UnitsValue</cref></description></item>
       ///  <item><term>Name</term><description><cref>System.String</cref></description></item>
       ///  <item><term>Position</term><description><cref>Autodesk.AutoCAD.Geometry.Point3d</cref></description></item>
       ///  <item><term>Rotation</term><description><cref>System.Double</cref></description></item>
       ///  <item><term>ScaleFactors</term><description><cref>Autodesk.AutoCAD.Geometry.Scale3d</cref></description></item>
       ///  <item><term>UnitFactor</term><description><cref>System.Double</cref></description></item>
       /// </list>
       /// </summary>
        public struct BlockInfo
        {
           public AttributeCollection AttributeCollection;
           public ObjectId BlockTableRecord;
           public UnitsValue BlockUnit;
           public Color Color;
           public bool IsDynamicBlock;
           public string Layer;
           public string Linetype;
           public double LinetypeScale;
           public LineWeight LineWeight;
           public string Name;
           public ObjectId ObjectId;
           public string PlotStyleName;
           public Point3d Position;
           public double Rotation;
           public Scale3d ScaleFactors;
           public Transparency Transparency;
           public double UnitFactor;
           public bool Visible;
        }

        //  CreateEntities
        /// <summary>
        /// A delegate method that accepts a collection of <see cref="Autodesk.AutoCAD.DatabaseServices.Entity"/> 
        /// objects and apppends its them to the specified  
        /// <see cref="Autodesk.AutoCAD.DatabaseServices.BlockTableRecord"/> in the active database.
        /// </summary>
        /// <param name="entities">An IEnumerable&lt;&gt; of entities to append.</param>
        public static void CreateEntities(IEnumerable<Entity> entities)
        {
           try
           {
              Active.Database.UsingCurrentSpace(
              (Transaction tr, BlockTableRecord btr) =>
              {
                 foreach (Entity ent in entities)
                 {
                    btr.AppendEntity(ent);
                    tr.AddNewlyCreatedDBObject(ent,true);
                 };
              });
           }
           catch //all exceptions
           {
              throw;
           }
        }

        //  ImportBlocksFromFile
        /// <summary>
        /// Imports block definitions from a dwg file.
        /// </summary>
        /// <param name="sourceFilePath">A <see cref="System.String"/> representing the path of dwg file to import as a block.</param>
        /// <param name="BlockName"><i>Optional: </i>Name of block to import. 
        /// If the parameter is missing or equals string.Empty or null, then all blocks are imported.</param>
        public static void ImportBlocksFromFile(string sourceFilePath, [Optional] string BlockName)
        {
            DocumentCollection dm = acApp.DocumentManager;
            Database destDb = Active.Database;
            Database sourceDb = new Database(false, true);

            try
            {
                // Read the DWG into a side database
                sourceDb.ReadDwgFile(sourceFilePath,
                                    System.IO.FileShare.Read,
                                    true,
                                    "");
                ObjectIdCollection colObjID = new ObjectIdCollection();
                sourceDb.ForEach<BlockTableRecord>(sourceDb.BlockTableId,  btr =>
                   {
                       // if BlockName == "" copy all
                      if (!btr.IsAnonymous && !btr.IsLayout && BlockName == string.Empty && BlockName == null)
                      {
                         colObjID.Add(btr.Id);
                      }
                      else if (BlockName != "" && BlockName == btr.Name)
                      {
                         colObjID.Add(btr.Id);                          
                      }
                   });
                  if (colObjID.Count > 0)
                  {
                     destDb.UsingTransaction(trans =>
                     {
                        IdMapping idMap = new IdMapping();
                        sourceDb.WblockCloneObjects(colObjID,destDb.BlockTableId,idMap,DuplicateRecordCloning.Ignore,true);
                     });
                  }               
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
               if (ex.Message == "eSelfReference")
               {
                  Active.WriteMessage("\nError during copy: " + BlockName + " references itself.");
               }
               else
               {
                  Active.WriteMessage("\nError during copy: " + ex.Message);
               }
            }
            sourceDb.Dispose();
        }

        //  ImportFileAsBlock
        /// <summary> 
        /// Import a dwg file as a block definition. This method does not insert the block definition.
        /// </summary>
        /// <param name="sourceFileName">path to file to import as a block</param>
        /// <param name="BlockName">Name assinged to block. Allows for renaming.
        ///  If = "", then the file name is used.</param>
        /// <returns><see cref="ObjectId"/> of the imported block's <see cref="BlockTableRecord"/>. Failure returns null.</returns>
        public static ObjectId ImportFileAsBlock(string sourceFilePath, string BlockName)
        {
            Document doc = Active.Document;
            Editor ed = Active.Editor;
            Database destDb = Active.Database;
            ObjectId btrId = new ObjectId();
            BlockTableRecord btr = new BlockTableRecord();
            
            try
            {
                // Read the DWG into a side database
                using(Database sourceDb = new Database(false, true))
                {
                    sourceDb.ReadDwgFile(sourceFilePath,
                                        System.IO.FileShare.Read,
                                        true,
                                        "");
                    bool isAnno = sourceDb.AnnotativeDwg;
                    if (BlockName.Equals(""))
                    {
                        BlockName = SymbolUtilityServices.GetSymbolNameFromPathName(sourceFilePath, "dwg;dxf");
                    }
                    // Check whether it works as a symbol table name.
                    // Validates name and any invalid characters are replaced.
                    BlockName = SymbolUtilityServices.RepairSymbolName(BlockName, false);

                    // Insert our drawing as a block (which will take the modelspace)
                    // blockID will remain null if this fails.
                    btrId = destDb.Insert(BlockName, sourceDb, true);
                    if (isAnno)
                    {
                        // If an annotative block, open the resultant BTR
                        // and set its annotative definition status
                        Transaction tr = destDb.TransactionManager.StartTransaction();
                        using (tr)
                        {
                            btr =
                              (BlockTableRecord)tr.GetObject(
                                btrId,
                                OpenMode.ForWrite
                              );
                            btr.Annotative = AnnotativeStates.True;
                            tr.Commit();
                        }
                    }
                }
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                ed.WriteMessage("\nError during import: " + ex.Message);
            }
            return btrId;

        }

        
        //  InsertFileAsBlock
        /// <summary>
        /// Insert a file as a block into the current dwg.
        /// </summary>
        /// <param name="sourceFilePath">A <see cref="System.String"/> representing the path of dwg file to insert.</param>
        /// <param name="BI">A <c>BlockInfo</c> object containing block insertion information.</param>
        /// <param name="BlockName">Name assigned to block. Allows for renaming.
        ///  If = "", then the file name is used.</param>
        /// <returns>ObjectId of the BlockReference just insterted.</returns>
        public static ObjectId InsertFileAsBlock(string sourceFilePath, BlockInfo BI, string BlockName)
        {            
            ObjectId NewBlkRefId = ObjectId.Null;
            ObjectId NewBlkId = ImportFileAsBlock(sourceFilePath, BlockName);
            if (NewBlkId != ObjectId.Null)
            {
                using (Transaction tr = NewBlkId.Database.TransactionManager.StartTransaction())
                {
                    BlockTableRecord btrCurrentSpace = (BlockTableRecord)tr.GetObject(NewBlkId.Database.CurrentSpaceId, OpenMode.ForWrite);
                    BlockReference brNewBlock = new BlockReference(BI.Position, NewBlkId)
                       {Rotation = BI.Rotation, ScaleFactors = BI.ScaleFactors, Layer = BI.Layer };
                    NewBlkRefId = btrCurrentSpace.AppendEntity(brNewBlock);
                    tr.AddNewlyCreatedDBObject(brNewBlock, true);
                    brNewBlock.AddAttributeReferences(tr);
                    tr.Commit();
                }
            }
            return NewBlkRefId;


        }

        //  RedefineAction
        /// <summary>
        /// An enumerator to specify how the <see cref="Redefine"/> method handles AttributeCollections. 
        /// </summary>
       public enum RedefineAction : int
        {
           /// <summary>No action taken on Attributes. The block will be redefined and
           /// the existing AttributeReference's will remain where they were.</summary>
           LeaveExisting,
           /// <summary>Old AttributeReference values are preseved and if
           /// there is a matching tag in the new block's AttributeCollection, 
           /// it's TagString will be updated with the value from the old block.</summary>
           PreserveAttValues,
           /// <summary>Remove all AttributeReference from the new block's AttributeCollection.</summary>
           RemoveAllAtts
        }
       
        //   SearchBlockTable
        /// <summary>
        /// SearchBlockTable enumerates the BlockTable and return a list of ID's
        /// of the block names searched for.
        /// </summary>
        /// <param name="BlockName">Name of block to search for.</param>
        /// <returns>ObjectIdCollection of blocks found.</returns>
        public static ObjectIdCollection SearchBlockTable(string BlockName)
        {
            /* Need to make sure that the current drawing is the one that you want to work on before calling this function. 
             Added by Jim Taylor 7/27/2009 */
            Database acadDB = acApp.DocumentManager.MdiActiveDocument.Database;
            ObjectIdCollection BLKoids = new ObjectIdCollection();
            ObjectIdCollection TMPoids = new ObjectIdCollection();
            BlockTableRecord BTR = new BlockTableRecord();
            int i;

            try
            {
                BlockTable BT = acadDB.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
                SymbolTableEnumerator BTE = BT.GetEnumerator();
                BTE.Reset();
                //BlockTableRecord BTR = BT[BlockName].GetObject(OpenMode.ForRead) as BlockTableRecord;

                while (BTE.MoveNext())
                {
                    BTR = BTE.Current.GetObject(OpenMode.ForRead) as BlockTableRecord;
                    if (BTR.Name.Equals(BlockName, StringComparison.OrdinalIgnoreCase))
                    {
                        TMPoids = BTR.GetBlockReferenceIds(true, true);
                        for (i = 1; i <= TMPoids.Count; i++)
                        {
                            BLKoids.Add(TMPoids[i - 1]);
                        }
                        // Exit While
                        break;
                    }
                }

                BTE.Reset();
                while (BTE.MoveNext())
                {
                    BTR = BTE.Current.GetObject(OpenMode.ForRead) as BlockTableRecord;
                    if (BTR.IsLayout == false)
                    {
                        switch (BTR.IsAnonymous)
                        {
                            case true:
                                TMPoids = BTR.GetBlockReferenceIds(true, true);
                                if (TMPoids.Count > 0)
                                {
                                    BlockReference blkRef = TMPoids[0].GetObject(OpenMode.ForRead) as BlockReference;
                                    BlockTableRecord BTR2 = blkRef.DynamicBlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord;
                                    if (BTR2.Name.Equals(BlockName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        for (i = 1; i <= TMPoids.Count; i++)
                                        {
                                            BLKoids.Add(TMPoids[i - 1]);
                                        }
                                    }
                                }
                                break;
                        }
                    }
                }
                return BLKoids;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                //Throw Exception
                MessageBox.Show("Error Occured in BlockIDs Function. " + ex.Message + ", " + ex.HelpLink, "BlockIDs Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return BLKoids;
            }
            finally
            {
                acadDB.Dispose();
                // Do not dispose of variables that you return 
                // as it will cause errors in your application.
                //BLKoids.Dispose();
                TMPoids.Dispose();
            }
        }

        

    }

    //  Files
    /// <summary>
    /// Static class collection of file manipulation utilities.
    /// </summary>
    public static class Files
    {
        
    
    }

    //  Misc
    /// <summary>
    /// Static class collection of miscellaneous utilities.
    /// </summary>
    public static class Misc
    {
        /// <GetLayouts>
        /// <summary>
        /// Gets all of the Layouts in the specified database.
        /// </summary>
        /// <param name="DB">Database to search for Layouts.</param>
        /// <returns>System.Collections.Generic.List&lt;Layout&gt; of the PaperSpace Layouts</returns>
        public static System.Collections.Generic.List<Layout> GetLayouts(Database DB)
        {
            ObjectIdCollection loids = new ObjectIdCollection();
            System.Collections.Generic.List<Layout> _Layouts = new List<Layout>();
            try
            {
                Transaction tr = DB.TransactionManager.StartTransaction();
                
                DBDictionary LayoutsDict = tr.GetObject(DB.LayoutDictionaryId, OpenMode.ForRead) as DBDictionary;
                foreach (DBDictionaryEntry entry in LayoutsDict)
                {
                    ObjectId lid = entry.Value;
                    Layout lay = tr.GetObject(lid, OpenMode.ForRead, false) as Layout;
                    _Layouts.Add(lay);
                }
                tr.Dispose();
                return _Layouts;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                //Throw Exception
                MessageBox.Show("Error Occured in GetLayouts Function. " + ex.Message + ", " + ex.HelpLink, "GetLayouts Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }



        /// <RemoveObjects>
        /// <summary>
        /// Removes objects in the passed databse
        /// </summary>
        /// <param name="DB">Database to delete objects from.</param>
        /// <param name="ids">Collection of object ID's to delete.</param>
        /// <returns>boolean indicating success.</returns>
        public static bool RemoveObjects( Database DB, ObjectIdCollection ids)
        {
            // Remove old Entities 
            if (ids.Count >= 1)
            {
                Transaction acadtrans = DB.TransactionManager.StartTransaction();
                try
                {
                    foreach (ObjectId blkid in ids)
                    {
                        // Erase Entities by ID
                        Entity E = (Entity)acadtrans.GetObject(blkid, OpenMode.ForWrite, true);
                        E.Erase();
                    }
                    BlockTable BT = (BlockTable)acadtrans.GetObject(DB.BlockTableId, OpenMode.ForRead);
                    ObjectIdCollection DBids = new ObjectIdCollection();
                    foreach (ObjectId btrid in BT)
                    {
                        DBids.Add(btrid);
                    }
                    // Remove Objectids of Entities that are not erased
                    DB.Purge(DBids);
                    // Remove Block Table Records
                    foreach (ObjectId id in DBids)
                    {
                        BlockTableRecord BTR = (BlockTableRecord)acadtrans.GetObject(id, OpenMode.ForWrite);
                        BTR.Erase(true);
                    }
                    acadtrans.Commit();
                    return true;
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    MessageBox.Show("Error Occured in TitleBlockChange Function. " + ex.Message + ", " + ex.HelpLink, "TitleBlockChange Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    acadtrans.Abort();
                    return false;
                }


            }
            return false;
        }
    
    }

    //  SelSets
    /// <summary>
    /// Static class collection of SelectionSet utilities.
    /// </summary>
    public static class SelSets
    {        
        

       //  CreateFilterListForBlocks
       /// <summary>
       /// Helper function to create a selection filter.
       /// </summary>
       /// <param name="blkNames">List&lt;string&gt; of block names to filter for.</param>
       /// <returns>Returns a TypedValue array of block names.</returns>
       /// <remarks><para>Suggested usage: </para>
       /// <para>SelectionFilter sf = new SelectionFilter(CreateFilterListForBlocks(blkNames));</para>
       /// <para>PromptSelectionResult psr = ed.SelectAll(sf);</para></remarks>
        public static TypedValue[] CreateFilterListForBlocks(List<string> blkNames)
        {
          // If we don't have any block names, return null
          if (blkNames.Count == 0)
            return null; 

          // If we only have one, return an array of a single value
          if (blkNames.Count == 1)
            return new TypedValue[] {new TypedValue((int)DxfCode.BlockName, blkNames[0])}; 

          // We have more than one block names to search for...
          // Create a list big enough for our block names plus
          // the containing "or" operators 

          List<TypedValue> tvl =
            new List<TypedValue>(blkNames.Count + 2); 

          // Add the initial operator
          tvl.Add(new TypedValue((int)DxfCode.Operator, "<or")); 

          // Add an entry for each block name, prefixing the
          // anonymous block names with a reverse apostrophe
          foreach (var blkName in blkNames)
          {
            tvl.Add(
              new TypedValue(
                (int)DxfCode.BlockName,
                (blkName.StartsWith("*") ? "`" + blkName : blkName)
              )
            );
          }

          // Add the final operator
          tvl.Add(new TypedValue((int)DxfCode.Operator, "or>")); 

          // Return an array from the list
          return tvl.ToArray();
        }


        //  CreateFilterListForLayers
        /// <summary>
        /// Helper function to create a selection filter.
        /// </summary>
        /// <param name="lyrNames">List&lt;string&gt; of layer names to filter for.</param>
        /// <returns>Returns a TypedValue array of block names.</returns>
        /// <remarks><para>Suggested usage: </para>
        /// <para>SelectionFilter sf = new SelectionFilter(CreateFilterListForLayers(lyrNames));</para>
        /// <para>PromptSelectionResult psr = ed.SelectAll(sf);</para></remarks>
        public static TypedValue[] CreateFilterListForLayers(List<string> lyrNames)
        {
            // If we don't have any block names, return null
            if (lyrNames.Count == 0)
                return null;

            // If we only have one, return an array of a single value
            if (lyrNames.Count == 1)
                return new TypedValue[] { new TypedValue((int)DxfCode.LayerName, lyrNames[0]) };

            // We have more than one block names to search for...
            // Create a list big enough for our block names plus
            // the containing "or" operators 

            List<TypedValue> tvl =
              new List<TypedValue>(lyrNames.Count + 2);

            // Add the initial operator
            tvl.Add(new TypedValue((int)DxfCode.Operator, "<or"));

            // Add an entry for each block name, prefixing the
            // anonymous block names with a reverse apostrophe
            foreach (var lyrName in lyrNames)
            {
                tvl.Add(
                  new TypedValue(
                    (int)DxfCode.BlockName,
                    (lyrName.StartsWith("*") ? "`" + lyrName : lyrName)
                  )
                );
            }

            // Add the final operator
            tvl.Add(new TypedValue((int)DxfCode.Operator, "or>"));

            // Return an array from the list
            return tvl.ToArray();
        }
    }

    
    //  Strings 
    /// <summary>
    /// Static class collection of Net String utilities.
    /// </summary>
    public static class Strings
    {
       //  TrimToNull
       /// <summary>
       /// Helper function to check if the iput string is all whitespace.
       /// </summary>
       /// <param name="source">Source string to compare.</param>
       /// <returns>Returns null if the input string is if all whitspace. 
       /// Otherwise the input string is returned</returns>
       public static string TrimToNull (string source)
       {
          return string.IsNullOrEmpty(source) ? null : source.Trim();
       }

       //  NullToDefault
       /// <summary>
       /// Helper function to check if the input string is null. 
       /// Returns a default string if it is.
       /// </summary>
       /// <param name="source">Source string to compare.</param>
       /// <param name="newDefault">Default string to use if source is null.</param>
       /// <returns>Returns if the default string if the source is null.
       public static string NullToDefault(string source, string newDefault)
       {
          return TrimToNull(source) ?? newDefault;
       }
    }
}
