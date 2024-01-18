using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using AcRx = Autodesk.AutoCAD.Runtime;
using Exception = System.Exception;

namespace TID_.Utilities
{

    /// <summary>
    ///   Extensions to Block related classes.
    /// </summary>
    public static class BlockExtensions
    {

        /// <summary>
        /// Extension method that adds <see cref="AttributeReference"/>'s to a <see cref="BlockReference"/>. 
        /// AttributeReference properties are taken from the block being inserted.
        /// </summary>
        /// <param name="target">A BlockReference to add the AttributeReferences to.</param>
        /// <param name="tr">A <see cref="Transaction"/> within which to add the attributes.</param>
        /// <returns>Void.</returns>
        public static void AddAttributeReferences(this BlockReference target, Transaction tr)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(target.BlockTableRecord, OpenMode.ForRead);

            if (btr.HasAttributeDefinitions)
            {
                foreach (AttributeDefinition attDef in btr.AttributeDefinitions(tr))
                {
                    if (!attDef.Constant)
                    {
                        using (AttributeReference attRef = new AttributeReference())
                        {
                            attRef.SetAttributeFromBlock(attDef, target.BlockTransform);
                            attRef.Position = attDef.Position.TransformBy(target.BlockTransform);
                            attRef.TextString = attDef.TextString;
                            target.AttributeCollection.AppendAttribute(attRef);
                            tr.AddNewlyCreatedDBObject(attRef, true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Extension method that returns adds <see cref="AttributeReference"/>'s to a <see cref="BlockReference"/>.
        /// AttributeReference values are taken from the Dictionary attValues argument.
        /// </summary>
        /// <param name="attValues">The Dictionary to get attribute values from. 
        /// The first string is the Key and is equal to the AttributeDefinition.Tagstring.
        /// The second string is the attribute value.</param>
        /// <returns>Void.</returns>
        public static void AddAttributeReferences(this BlockReference target, Dictionary<string, string> attValues)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            Transaction tr = target.Database.TransactionManager.TopTransaction;
            if (tr == null)
                throw new AcRx.Exception(ErrorStatus.NoActiveTransactions);

            BlockTableRecord btr = (BlockTableRecord)tr.GetObject(target.BlockTableRecord, OpenMode.ForRead);

            foreach (AttributeDefinition attDef in btr.AttributeDefinitions(tr))
            {
                if (!attDef.Constant)
                { 
                    AttributeReference attRef = new AttributeReference();
                    attRef.SetAttributeFromBlock(attDef, target.BlockTransform);
                    if (attValues != null && attValues.ContainsKey(attDef.Tag.ToUpper()))
                    {
                        attRef.TextString = attValues[attDef.Tag.ToUpper()];
                    }
                    target.AttributeCollection.AppendAttribute(attRef);
                    tr.AddNewlyCreatedDBObject(attRef, true);                
                }
            }
        }

        /// <summary>
        /// Extension method that returns a <see cref="Collection"/> of <see cref="AttributeDefinition"/>'s from a <see cref="BlockTableRecord"/>.
        /// </summary>
        /// <param name="btr">This BlockTableRecord to use.</param>
        /// <param name="trans">The Transaction within which to pass the Collection of AttributeDefinitions.</param>
        /// <returns>A Collection of AttributeDefinitions.</returns>
        public static Collection<AttributeDefinition> AttributeDefinitions(this BlockTableRecord btr, Transaction trans)
        {
            Collection<AttributeDefinition> atts = new Collection<AttributeDefinition>();

            RXClass theClass = RXObject.GetClass(typeof(AttributeDefinition));

            // Loop through the entities in model space
            foreach (ObjectId objectId in btr.Cast<ObjectId>())
            {
                // Look for entities of the correct type
                if (objectId.ObjectClass.IsDerivedFrom(theClass))
                {
                    AttributeDefinition attd = objectId.OpenAs<AttributeDefinition>(trans);
                    atts.Add(attd);
                }
            }
            return atts;
        }

        /// <summary>
        /// Extension method that clears all AttributeReferences from an AttributeCollection. 
        /// </summary>
        /// <param name="ac">The <see cref="AttributeCollection"/> to process</param>
        /// <returns>Void.</returns>
        public static void Clear(this AttributeCollection ac)
        {
            foreach (ObjectId attId in ac)
            {
                // This test is due to a bug in the api.
                // The item at index = 0 will not clear from the collection.
                if (!attId.IsErased)
                {
                    AttributeReference attref = attId.GetObject(OpenMode.ForWrite) as AttributeReference;
                    attref.Erase(true);
                }
            }
        }

        /// <summary>
        /// Extension method that populates this <see cref="BlockReference"/>'s <see cref="AttributeCollection"/> 
        /// from a <see cref="BlockTableRecord"/> 
        /// using it's <see cref="AttributeDefinition"/>'s as the source.
        /// </summary>
        /// <param name="br">This BlockReference to use.</param>
        /// <param name="btr">The BlockTableRecord to get the definitions from.</param>
        /// <param name="tr">The transaction.</param>
        /// <returns>Dictionary&lt;ObjectId, AttributeReference&gt;</returns>
        public static Dictionary<ObjectId, AttributeReference> CreateAttRefCollection(this BlockReference br, BlockTableRecord btr, Transaction tr)
        {
            Dictionary<ObjectId, AttributeReference> attInfo = new Dictionary<ObjectId, AttributeReference>();

            if (btr.HasAttributeDefinitions)
            {
                foreach (ObjectId id in btr)
                {
                    DBObject obj =
                      tr.GetObject(id, OpenMode.ForRead);
                    AttributeDefinition ad = obj as AttributeDefinition;

                    if (ad != null && !ad.Constant)
                    {
                        AttributeReference ar = new AttributeReference();

                        ar.SetAttributeFromBlock(ad, br.BlockTransform);
                        ar.Position = ad.Position.TransformBy(br.BlockTransform);


                        if (ad.Justify != AttachmentPoint.BaseLeft)
                        {
                            ar.AlignmentPoint = ad.AlignmentPoint.TransformBy(br.BlockTransform);
                        }
                        if (ar.IsMTextAttribute)
                        {
                            ar.UpdateMTextAttribute();
                        }
                        ar.TextString = ad.TextString;
                        ObjectId arId = br.AttributeCollection.AppendAttribute(ar);
                        tr.AddNewlyCreatedDBObject(ar, true);

                        // Initialize our dictionary with the ObjectId of
                        // the attribute reference + attribute definition info
                        attInfo.Add(arId, ar);
                    }
                }
            }
            return attInfo;
        }


        // GetAttInfo
        /// <summary>
        /// Extension method that populates this <see cref="BlockReference"/>'s <see cref="AttributeCollection"/> 
        /// from a <see cref="BlockTableRecord"/> 
        /// using it's <see cref="AttributeDefinition"/>'s as the source.
        /// </summary>
        /// <param name="br">This BlockReference to use.</param>
        /// <param name="btr">The BlockTableRecord to get the definitions from.</param>
        /// <param name="tr">The transaction.</param>
        /// <returns>Dictionary&lt;ObjectId, AttInfo&gt;</returns>
        public static Dictionary<ObjectId, AttInfo> GetAttInfo(this AttributeCollection ac, Transaction tr)
        {
            Dictionary<ObjectId, AttInfo> attInfo = new Dictionary<ObjectId, AttInfo>();

            if (ac.Count > 0)
            {
                foreach (ObjectId id in ac)
                {
                    // This test is due to a bug in the api.
                    // The item at index = 0 will not clear from the collection.
                    if (!id.IsErased)
                    {
                        DBObject obj = tr.GetObject(id, OpenMode.ForRead);
                        AttributeReference ar = obj as AttributeReference;

                        if (ar != null)
                        {
                            // Initialize our dictionary with the ObjectId of
                            // the attribute reference + attribute info
                            attInfo.Add(
                              ar.ObjectId,
                              new AttInfo(
                                ar.Position,
                                ar.AlignmentPoint,
                                ar.Justify != AttachmentPoint.BaseLeft,
                                ar.Tag,
                                ar.TextString
                              )
                            );
                        }
                    }
                }
            }
            return attInfo;
        }

        // GetAtts
        /// <summary>
        /// Extension method that gets <see cref="AttributeReference"/>'s from 
        /// this <see cref="AttributeCollection"/>.
        /// </summary>
        /// <param name="ac">This AttributeCollection to get attributes from.</param>
        /// <param name="tr">The transaction within which processign is done.</param>
        /// <returns>IEnumerable&lt;AttributeReference&gt;</returns>
        public static IEnumerable<AttributeReference> GetAtts(this AttributeCollection ac)
        {
            return ac
               .Cast<ObjectId>()
               .OfType<AttributeReference>();
        }

        public static ObjectId GetBlock(this BlockTable blockTable, string blockName)
        {
            if (blockTable == null)
                throw new ArgumentNullException("blockTable");

            Database db = blockTable.Database;
            if (blockTable.Has(blockName))
                return blockTable[blockName];

            try
            {
                string ext = Path.GetExtension(blockName);
                if (ext == "")
                    blockName += ".dwg";
                string blockPath;
                if (File.Exists(blockName))
                    blockPath = blockName;
                else
                    blockPath = HostApplicationServices.Current.FindFile(blockName, db, FindFileHint.Default);

                blockTable.UpgradeOpen();
                using (Database tmpDb = new Database(false, true))
                {
                    tmpDb.ReadDwgFile(blockPath, FileShare.Read, true, null);
                    return blockTable.Database.Insert(Path.GetFileNameWithoutExtension(blockName), tmpDb, true);
                }
            }
            catch
            {
                return ObjectId.Null;
            }
        }

        //  GetBlockInfo
        /// <summary>
        /// Extension method that gets the block properties of 
        /// a <see cref="Autodesk.AutoCAD.DatabaseServices.BlockReference"/> in the active database.
        /// </summary>
        /// <returns><c>BlockInfo</c> object containing the properties of the requested block.</returns>
        public static Blocks.BlockInfo GetBlockInfo(this BlockReference blkRef)
        {
            Blocks.BlockInfo BI = new Blocks.BlockInfo();
            Transaction tx = Active.Database.TransactionManager.StartTransaction();

            try
            {
                blkRef = tx.GetObject(blkRef.ObjectId, OpenMode.ForRead) as BlockReference;
                BI.AttributeCollection = blkRef.AttributeCollection;
                BI.BlockTableRecord = blkRef.BlockTableRecord;
                BI.BlockUnit = blkRef.BlockUnit;
                BI.Color = blkRef.Color;
                BI.IsDynamicBlock = blkRef.IsDynamicBlock;
                BI.Layer = blkRef.Layer;
                BI.Linetype = blkRef.Linetype;
                BI.LinetypeScale = blkRef.LinetypeScale;
                BI.LineWeight = blkRef.LineWeight;
                BI.Name = blkRef.Name;
                BI.ObjectId = blkRef.ObjectId;
                BI.PlotStyleName = blkRef.PlotStyleName;
                BI.Position = blkRef.Position;
                BI.Rotation = blkRef.Rotation;
                BI.ScaleFactors = blkRef.ScaleFactors;
                BI.Transparency = blkRef.Transparency;
                BI.UnitFactor = blkRef.UnitFactor;
                BI.Visible = blkRef.Visible;
                tx.Commit();
                return BI;
            }
            catch //all exceptions
            {
                //cleanup and rethrow the exception
                tx.Abort();
                throw;
            }
            finally
            {
                tx.Dispose();
            }
        }

        //  GetBlockTableRecord
        /// <summary>
        /// Extension method that gets the <see cref="Autodesk.AutoCAD.DatabaseServices.BlockTableRecord"/> of 
        /// a <see cref="Autodesk.AutoCAD.DatabaseServices.BlockReference"/> in the active database.
        /// </summary>
        /// <returns><see cref="Autodesk.AutoCAD.DatabaseServices.BlockTableRecord"/> object of the requested BlockReference.</returns>
        public static BlockTableRecord GetBlockTableRecord(this BlockReference blkRef)
        {
            Transaction tx = Active.Database.TransactionManager.StartTransaction();

            try
            {
                BlockTable bt = tx.GetObject(Active.Database.BlockTableId, OpenMode.ForRead, false, true) as BlockTable;
                BlockTableRecord btr = tx.GetObject(bt[blkRef.Name], OpenMode.ForRead) as BlockTableRecord;
                tx.Commit();
                return btr;
            }
            catch //all exceptions
            {
                //cleanup and rethrow the exception
                tx.Abort();
                throw;
            }
            finally
            {
                tx.Dispose();
            }
        }

        //GetXrefStatus
        /// <summary>
        /// Used to get status of a BlockTableRecord.
        /// </summary>
        /// <typeparam name="TObject">The type of the object.</typeparam>
        /// <param name="btr">The block table record.</param>
        /// <returns>A string value of the xref status.</returns>
        public static string GetXrefStatus(this BlockTableRecord btr)
        {
            switch (btr.XrefStatus)
            {
                case XrefStatus.FileNotFound:
                    return "File not found";
                case XrefStatus.Resolved:
                    return "Resolved";
                case XrefStatus.Unloaded:
                    return "Unloaded";
                case XrefStatus.Unreferenced:
                    return "Unreferenced";
                default:
                    return "Unresolved";
            }
        }

        //InsertBlockReference
        /// <summary>
        /// Insert a block into the current space. You can optionally fill attribute values.
        /// </summary>
        /// <param name="target">The BlockTableRecord of the (model/paper) space to insert the block in.</param>
        /// <param name="blkName">A string of the block's name.</param>
        /// <param name="insertPoint">A Point3d to be used as the insertion point.</param>
        /// <param name="attValues">A Dictionary&gtstring, string&lt in the form of Key, value. 
        /// To be used to supply tag string values. If a key is not found for an attribute, 
        /// the value from the AttributeDefinition is used.</param>
        /// <returns><see cref="Autodesk.AutoCAD.DatabaseServices.BlockReference"/> of the inserted block.</returns>
        public static BlockReference InsertBlockReference(
                                        this BlockTableRecord target, 
                                        string blkName, 
                                        Point3d insertPoint,    
                                        Dictionary<string, string> attValues = null)
        {
            if (target == null)
                throw new ArgumentNullException("target");

            Database db = target.Database;
            Transaction tr = db.TransactionManager.TopTransaction;
            if (tr == null)
                throw new AcRx.Exception(ErrorStatus.NoActiveTransactions);

            BlockReference br = null;
            BlockTable bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);

            ObjectId btrId = bt.GetBlock(blkName);

            if (btrId != ObjectId.Null)
            {
                br = new BlockReference(insertPoint, btrId);
                BlockTableRecord btr = (BlockTableRecord)tr.GetObject(btrId, OpenMode.ForRead);
                target.AppendEntity(br);
                tr.AddNewlyCreatedDBObject(br, true);

                br.AddAttributeReferences(attValues);
            }
            return br;
        }

        //  Redefine
        /// <summary>
        /// Extension method for a BlockReference object to redefine its Block object using the file specified. 
        /// If the optional name argument is supplied, it will also be renamed.
        /// </summary>
        /// <param name="blkRef">The type that this extension refers to.</param>
        /// <param name="trans">The current transaction the BlockReference is using.</param>
        /// <param name="path"><see cref="System.String"/> object pointing to the 
        ///  dwg file which will be the new block definition.</param>
        /// <param name="flag">A <see cref="RedifineAction"/> enum. If the block doesn't have any AttributeReferences, 
        ///  this flag has no effect.</param>
        /// <param name="blockname">Optional <see cref="System.String"/> used to rename the block.</param>
        /// <remarks>Note: This method handles attributes according to the value of the flag input parameter as listed below.
        /// <list type="bullet"> 
        ///   <item><term>RedefineAction.LeaveExisting</term>
        ///         <description>No action taken on Attributes. The block will be redefined and the 
        ///   existing AttributeReference's will remain where they were.</description></item>
        ///   <item><term>RedefineAction.PreserveAttValues</term>
        ///         <description>Old AttributeReference values are preseved and if there is a matching 
        ///   tag in the new block's AttributeCollection, it's TagString will be updated with 
        ///   the value from the old block.</description></item>
        ///   <item><term>RedefineAction.RemoveAllAtts</term>
        ///         <description>Remove all AttributeReference from the new block's AttributeCollection.</description></item>
        /// </list>
        /// Since this is an extension method, the assumption is that blkRef is open for write.
        /// Hence a transaction is ongoing and the changes made here will be committed in the parent transaction.
        /// Error handling to be done by caller.
        /// </remarks>
        public static void Redefine(this BlockReference blkRef,
                                    Transaction trans,
                                    string path,
                                    Blocks.RedefineAction flag,
                                    string blockname = null)
        {

            // store correct name
            string newName = "";
            if (string.IsNullOrEmpty(blockname))
            {
                //use filename as blockname
                //assume path is .dwg
                newName =  Path.GetFileNameWithoutExtension(path);
            }
            else
            {
                newName = blockname;
            }

            // Import the block to a temp name.
            ObjectId newBlkId = Blocks.ImportFileAsBlock(path, newName + "_temp");
            BlockTableRecord newBtr = newBlkId.GetObject(OpenMode.ForWrite, false, true) as BlockTableRecord;
            newBtr.UpgradeOpen();

            // Get a collection of the references using this same block
            BlockTableRecord btr = blkRef.GetBlockTableRecord();
            ObjectIdCollection brefIDs = btr.GetBlockReferenceIds(false, true);

            // swap in new block def and handle attributes
            switch (flag)
            {
                case Blocks.RedefineAction.LeaveExisting:
                    // Don't worry about atts. Just swap block def.
                    foreach (ObjectId id in brefIDs)
                    {
                        BlockReference bref = id.GetObject(OpenMode.ForWrite) as BlockReference;
                        bref.BlockTableRecord = newBtr.ObjectId;
                        bref.RecordGraphicsModified(true);
                    }
                    break;
                case Blocks.RedefineAction.PreserveAttValues:
                    // Save old att tag values, import new att collection, swap block.
                    foreach (ObjectId id in brefIDs)
                    {
                        BlockReference bref = id.GetObject(OpenMode.ForWrite) as BlockReference;
                        if (btr.HasAttributeDefinitions)
                        {
                            // first, store old att values
                            Dictionary<ObjectId, AttInfo> OldAtts = blkRef.AttributeCollection.GetAttInfo(trans);
                            //clear old atts
                            bref.AttributeCollection.Clear();
                            // import new atts from new Block def
                            Dictionary<ObjectId, AttributeReference> newAtts = bref.CreateAttRefCollection(newBtr, trans);
                            // update new TextString from old one.
                            foreach (AttributeReference att in newAtts.Values)
                            {
                                if (!att.IsErased)
                                {
                                    // update text values
                                    try
                                    {
                                        AttInfo ati = (from AttInfo ai in OldAtts.Values
                                                       where att.Tag == ai.Tag
                                                       select ai).Single();
                                        att.TextString = ati.TextString;

                                    }
                                    // Skip if old att does not exist, e.g. when redefining using a block
                                    // that has more atts than the old block did. The extra atts will 
                                    // end up being blank.
                                    catch (System.InvalidOperationException ex)
                                    {
                                        // ignore error and continue processing
                                        att.TextString = "";
                                        string msg = ex.Message;
                                    }
                                }
                            }
                        }
                        bref.BlockTableRecord = newBtr.ObjectId;
                        bref.RecordGraphicsModified(true);
                    }
                    break;
                case Blocks.RedefineAction.RemoveAllAtts:
                    // Delete all atts and swap block def.
                    foreach (ObjectId id in brefIDs)
                    {
                        BlockReference bref = id.GetObject(OpenMode.ForWrite) as BlockReference;
                        if (btr.HasAttributeDefinitions)
                        {
                            bref.AttributeCollection.Clear();
                        }
                        bref.BlockTableRecord = newBtr.ObjectId;
                        bref.RecordGraphicsModified(true);
                    }
                    break;
                default:
                    break;
            }



            // Now delete the old block and rename the temp to the orig name.
            btr.Erase();
            btr = newBtr;
            btr.Name = newName;

            //foreach (AttributeReference newAtt in newbBlk.AttributeCollection)
            //{
            //   AttributeReference att = BI.AttributeCollection.Attributes()[newAtt.Tag.ToString()];
            //   newAtt.TextString = att.TextString; 
            //};
        }


    }

	/// <summary>
	/// Contains extension methods that facilitate working with objects in the context of a transaction.
	/// </summary>
	public static class DbExtensions
   {

		/// <summary>
		/// Extension method that allows you to iterate through model space and perform an action
		/// on a specific type of object.
		/// </summary>
		/// <typeparam name="T">The type of object to search for.</typeparam>
		/// <param name="db">The database to use.</param>
		/// <param name="action">A delegate that is called for each object found of the specified type.</param>
		public static void ForEach<T>(this Database db, Action<T> action)
			where T : Entity
		{
			db.UsingModelSpace((tr, modelSpace) => modelSpace.ForEach(tr, action));
		}

		/// <summary>
		/// Extension method that allows you to iterate through model space and perform an action
		/// on a specific type of object.
		/// </summary>
		/// <typeparam name="T">The type of object to search for.</typeparam>
		/// <param name="db">The database to use.</param>
		/// <param name="predicate"></param>
		/// <param name="action">A delegate that is called for each object found of the specified type.</param>
		public static void ForEach<T>(this Database db, Func<T, bool> predicate, Action<T> action)
			where T : Entity
		{
			db.ForEach<T>(
				obj =>
					{
						if (predicate(obj))
							action(obj);
					});
		}

		/// <summary>
		/// Iterates through the specified symbol table, and performs an action on each symbol table record.
		/// </summary>
		/// <typeparam name="T">The type of symbol table record.</typeparam>
		/// <param name="db">The database.</param>
		/// <param name="tableId">The table id.</param>
		/// <param name="action">A delegate that is called for each record.</param>
		public static void ForEach<T>(this Database db, ObjectId tableId, Action<T> action) where T : SymbolTableRecord
		{
			db.UsingTransaction(tr => tableId.OpenAs<SymbolTable>(tr).Cast<ObjectId>().ForEach(tr, action));
		}

		/// <summary>
		/// Extension method that allows you to iterate through model space and perform an action
		/// on a specific type of object.
		/// </summary>
		/// <typeparam name="T">The type of object to search for.</typeparam>
		/// <param name="document">The document to use.</param>
		/// <param name="action">A delegate that is called for each object found of the specified type.</param>
		/// <remarks>This method locks the specified document.</remarks>
		public static void ForEach<T>(this Document document, Action<T> action)
			where T : Entity
		{
			using (document.LockDocument())
			{
				document.Database.ForEach(action);
			}
      }

		/// <summary>
		/// Extension method that allows you to iterate through the objects in a block table
		/// record and perform an action on a specific type of object.
		/// </summary>
		/// <typeparam name="T">The type of object to search for.</typeparam>
		/// <param name="btr">The block table record to iterate.</param>
		/// <param name="tr">The active transaction.</param>
		/// <param name="action">A delegate that is called for each object found of the specified type.</param>
		public static void ForEach<T>(this IEnumerable<ObjectId> btr, Transaction tr, Action<T> action)
			where T : DBObject
		{
			RXClass theClass = RXObject.GetClass(typeof(T));

			// Loop through the entities in model space
			foreach (ObjectId objectId in btr)
			{
				// Look for entities of the correct type
				if (objectId.ObjectClass.IsDerivedFrom(theClass))
				{
					action(objectId.OpenAs<T>(tr));
				}
			}
		}

        /// <summary>
        /// Extension method that returns the LayerTableRecord's of the given database.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="tr">A <see>Autodesk.AutoCAD.DatabaseServices.Transaction</see> within which to read the LayerTable.</param>
        /// <returns>An enumerable list of <see>Autodesk.AutoCAD.DatabaseServices.LayerTableRecord</see> objects.</returns>
        public static IEnumerable<LayerTableRecord> Layers(this Database database, Transaction tr)
        {
            return database.LayerTableId
               .OpenAs<LayerTable>(tr)
               .Cast<ObjectId>()
               .OfType<LayerTableRecord>();
        }

        /// <summary>
        /// Opens a database-resident object as the specified type within the context of the specified transaction,
        /// using the specified open mode.
        /// </summary>
        /// <typeparam name="T">The type of object that the objectId represents.</typeparam>
        /// <param name="objectId">The object id.</param>
        /// <param name="tr">The transaction.</param>
        /// <param name="openMode">The open mode.</param>
        /// <returns>The database-resident object.</returns>
        public static T OpenAs<T>(this ObjectId objectId, Transaction tr, OpenMode openMode)
         where T : DBObject
      {
         return (T)tr.GetObject(objectId, openMode);
      }

		/// <summary>
		/// Locks the document, opens the specified object, and passes it to the specified delegate.
		/// </summary>
		/// <typeparam name="T">The type of object the objectId represents.</typeparam>
		/// <param name="document">The document.</param>
		/// <param name="objectId">The object id.</param>
		/// <param name="openMode">The open mode.</param>
		/// <param name="action">A delegate that takes the opened object as an argument.</param>
		public static void OpenAs<T>(this Document document, ObjectId objectId, OpenMode openMode, Action<T> action)
			where T : DBObject
		{
			document.UsingTransaction(tr => action(objectId.OpenAs<T>(tr, openMode)));
      }

      /// <summary>
      /// Opens a database-resident object as the specified type (for read) within the context of the specified transaction.
      /// </summary>
      /// <typeparam name="T">The type of object that the objectId represents.</typeparam>
      /// <param name="objectId">The object id.</param>
      /// <param name="tr">The transaction.</param>
      /// <returns>The database-resident object.</returns>
      public static T OpenAs<T>(this ObjectId objectId, Transaction tr)
         where T : DBObject
      {
         return (T)tr.GetObject(objectId, OpenMode.ForRead);
      }

      /// <summary>
      /// Opens a database-resident object as the specified type, using the specifed OpenMode within the context of the specified delegate.
      /// </summary>
      /// <typeparam name="T">The type of object that the objectId represents.</typeparam>
      /// <param name="objectId">The object id.</param>
      /// <param name="openMode">An <see>Autodesk.Autocad.DatabaseServices.OpenMode</see> constant.</param>
      /// <param name="action">A delegate that takes the transaction and the OpenMode as arguments.</param>
		public static void OpenAs<T>(this ObjectId objectId, OpenMode openMode, Action<T> action)
			where T : DBObject
		{
			objectId.Database.UsingTransaction(tr => action(objectId.OpenAs<T>(tr, openMode)));
		}

      /// <summary>
      /// Opens a database-resident object as the specified type (for write) within the context of the specified delegate.
      /// </summary>
      /// <typeparam name="T">The type of object that the objectId represents.</typeparam>
      /// <param name="objectId">The object id.</param>
      /// <param name="action">A delegate that takes a transaction as an argument.</param>
		public static void OpenForWriteAs<T>(this ObjectId objectId, Action<T> action)
			where T : DBObject
		{
			objectId.Database.UsingTransaction(tr => action(objectId.OpenAs<T>(tr, OpenMode.ForWrite)));
      }

      /// <summary>
      /// Used to get a single value from a database-resident object.
      /// </summary>
      /// <typeparam name="TObject">The type of the object.</typeparam>
      /// <typeparam name="TResult">The type of the result.</typeparam>
      /// <param name="objectId">The object id.</param>
      /// <param name="func">A delegate that takes the object as an argument and returns the value.</param>
      /// <returns>A value of the specified type.</returns>
      public static TResult GetValue<TObject, TResult>(this ObjectId objectId, Func<TObject, TResult> func)
         where TObject : DBObject
      {
         TResult result = default;

         objectId.Database.UsingTransaction(
            tr =>
            {
               result = func(objectId.OpenAs<TObject>(tr));
            });

         return result;
      }


      /// <summary>
      /// Executes a delegate function in the context of a transaction, and passes it the specified block table record.
      /// </summary>
      /// <param name="database">The database.</param>
      /// <param name="blockName">Name of the block.</param>
      /// <param name="action">A delegate that takes the transaction and the BlockTableRecord as arguments.</param>
      public static void UsingBlockTable(this Database database, string blockName, Action<Transaction, BlockTableRecord> action)
      {
         database.UsingTransaction(
            tr =>
            {
               // Get the block table
               var blockTable = database.BlockTableId.OpenAs<BlockTable>(tr);

               // Get the block table record
               var tableRecord = blockTable[blockName].OpenAs<BlockTableRecord>(tr);

               // Invoke the method
               action(tr, tableRecord);
            });
      }

      /// <summary>
      /// Executes a delegate function in the context of a transaction, and passes it the collection
      /// of Entity objects for the specified block table record.
      /// </summary>
      /// <param name="database">The database.</param>
      /// <param name="blockName">Name of the block.</param>
      /// <param name="action">A delegate that takes the transaction and the Entity collection as arguments.</param>
      public static void UsingBlockTable(this Database database, string blockName, Action<IEnumerable<Entity>> action)
      {
         database.UsingBlockTable
            (blockName,
             (tr, blockTable) => action(from id in blockTable select id.OpenAs<Entity>(tr)));
      }

      /// <summary>
      /// Executes a delegate function in the context of a transaction, and passes it the collection
      /// of ObjectIds for the specified block table record.
      /// </summary>
      /// <param name="database">The database.</param>
      /// <param name="blockName">Name of the block.</param>
      /// <param name="action">A delegate that takes the transaction and the ObjectIds as arguments.</param>
      public static void UsingBlockTable(this Database database, string blockName, Action<Transaction, IEnumerable<ObjectId>> action)
      {
         database.UsingTransaction(
            tr =>
            {
               // Get the block table
               var blockTable = database.BlockTableId.OpenAs<BlockTable>(tr);

               // Get the block table record
               var tableRecord = blockTable[blockName].OpenAs<BlockTableRecord>(tr);

               // Invoke the method
               action(tr, tableRecord.Cast<ObjectId>());
            });
      }

      /// <summary>
      /// Executes a delegate function in the context of a transaction, 
      /// and passes it the block table record of the current space.
      /// </summary>
      /// <param name="database">The database.</param>
      /// <param name="blockName">Name of the block.</param>
      /// <param name="action">A delegate that takes the transaction and the BlockTableRecord as arguments.</param>
      public static void UsingCurrentSpace(this Database database, Action<Transaction, BlockTableRecord> action)
      {
         database.UsingTransaction(
            tr =>
            {
               // Get the block table
               var currentSpaceTableRec = database.CurrentSpaceId.OpenAs<BlockTableRecord>(tr, OpenMode.ForWrite);

               // Invoke the method
               action(tr, currentSpaceTableRec);
            });
      }

      /// <summary>
      /// Executes a delegate function in the context of a transaction, and passes it the collection
      /// of ObjectIds for the current space block table record.
      /// </summary>
      /// <param name="database">The database.</param>
      /// <param name="blockName">Name of the block.</param>
      /// <param name="action">A delegate that takes the transaction and the ObjectId's as arguments.</param>
      public static void UsingCurrentSpace(this Database database, Action<Transaction, IEnumerable<ObjectId>> action)
      {
         database.UsingTransaction(
            tr =>
            {
               // Get the block table
               var currentSpaceTableRec = database.CurrentSpaceId.OpenAs<BlockTableRecord>(tr,OpenMode.ForWrite);

               // Invoke the method
               action(tr, currentSpaceTableRec.Cast<ObjectId>());
            });
      }

      /// <summary>
      /// Executes a delegate function in the context of a transaction, and passes it the collection 
      /// of objects from the current space of the specified type.
      /// </summary>
      /// <param name="database">The database.</param>
      /// <param name="action">A delegate that takes the transaction and the Entity collection as arguments.</param>
      /// <typeparamref name="T">The type of object to retrieve.</typeparamref>
      public static void UsingCurrentSpace<T>(this Database database, Action<IEnumerable<T>> action) where T : Entity
      {
         database.UsingCurrentSpace(
            (tr, csOIDs) =>
            {
               RXClass rxClass = RXObject.GetClass(typeof(T));

               action(from id in csOIDs
                      where id.ObjectClass.IsDerivedFrom(rxClass)
                      select id.OpenAs<T>(tr));
            });
      }

      /// <summary>
      /// Executes a delegate function with the collection of layers in the specified database.
      /// </summary>
      /// <param name="database">The database.</param>
      /// <param name="action">A delegate that takes the collection of layers as an argument.</param>
      public static void UsingLayerTable(this Database database, Action<IEnumerable<LayerTableRecord>> action)
      {
         database.UsingTransaction(
            tr => action(from ObjectId id in database.LayerTableId.OpenAs<LayerTable>(tr)
                         select id.OpenAs<LayerTableRecord>(tr)));
      }

      /// <summary>
      /// Executes a delegate function in the context of a transaction, and passes it the collection
      /// of objects from model space of the specified type.
      /// </summary>
      /// <param name="database">The database.</param>
      /// <param name="action">A delegate that takes the transaction and the Entity collection as arguments.</param>
      /// <typeparamref name="T">The type of object to retrieve.</typeparamref>
      public static void UsingModelSpace<T>(this Database database, Action<IEnumerable<T>> action) where T : Entity
      {
         database.UsingModelSpace(
            (tr, ms) =>
            {
               RXClass rxClass = RXObject.GetClass(typeof(T));

               action(from id in ms
                      where id.ObjectClass.IsDerivedFrom(rxClass)
                      select id.OpenAs<T>(tr));
            });
      }

      /// <summary>
      /// Executes a delegate function in the context of a transaction, and passes it the model space block table record.
      /// </summary>
      /// <param name="database">The database.</param>
      /// <param name="action">A delegate that takes the transaction and the ObjectId's as arguments.</param>
      public static void UsingModelSpace(this Database database, Action<Transaction, BlockTableRecord> action)
      {
         database.UsingBlockTable(BlockTableRecord.ModelSpace, action);
      }

      /// <summary>
      /// Executes a delegate function in the context of a transaction, and passes it the collection
      /// of ObjectIds for the model space block table record.
      /// </summary>
      /// <param name="database">The database.</param>
      /// <param name="action">A delegate that takes the transaction and the ObjectIds as arguments.</param>
      public static void UsingModelSpace(this Database database, Action<Transaction, IEnumerable<ObjectId>> action)
      {
         database.UsingBlockTable(BlockTableRecord.ModelSpace, action);
      }

      /// <summary>
      /// Executes a delegate function in the context of a transaction, and passes it the collection
      /// of objects from paper space of the specified type.
      /// </summary>
      /// <param name="database">The database.</param>
      /// <param name="action">A delegate that takes the transaction and the Entity collection as arguments.</param>
      /// <typeparamref name="T">The type of object to retrieve.</typeparamref>
      public static void UsingPaperSpace<T>(this Database database, Action<IEnumerable<T>> action) where T : Entity
      {
         database.UsingPaperSpace(
            (tr, ps) =>
            {
               RXClass rxClass = RXObject.GetClass(typeof(T));

               action(from id in ps
                      where id.ObjectClass.IsDerivedFrom(rxClass)
                      select id.OpenAs<T>(tr));
            });
      }

      /// <summary>
      /// Executes a delegate function in the context of a transaction, and passes it the paper space block table record.
      /// </summary>
      /// <param name="database">The database.</param>
      /// <param name="action">A delegate that takes the transaction and the ObjectIds as arguments.</param>
      public static void UsingPaperSpace(this Database database, Action<Transaction, BlockTableRecord> action)
      {
         database.UsingBlockTable(BlockTableRecord.PaperSpace, action);
      }

      /// <summary>
      /// Executes a delegate function in the context of a transaction, and passes it the collection
      /// of ObjectIds for the paper space block table record.
      /// </summary>
      /// <param name="database">The database.</param>
      /// <param name="action">A delegate that takes the transaction and the ObjectIds as arguments.</param>
      public static void UsingPaperSpace(this Database database, Action<Transaction, IEnumerable<ObjectId>> action)
      {
         database.UsingBlockTable(BlockTableRecord.PaperSpace, action);
      }

      /// <summary>
      /// Executes a delegate function within the context of a transaction on the specified database.
      /// </summary>
      /// <param name="database">The database.</param>
      /// <param name="action">A delegate that takes the <b>Transaction</b> as an argument.</param>
      public static void UsingTransaction(this Database database, Action<Transaction> action)
      {
         using (var tr = database.TransactionManager.StartTransaction())
         {
            try
            {
               action(tr);
               tr.Commit();
            }
            catch (Exception)
            {
               tr.Abort();
               throw;
            }
         }
      }

      /// <summary>
      /// Executes a delegate function within the context of a transaction on the specified document.
      /// The document is locked before the transaction starts.
      /// </summary>
      /// <param name="document">The document.</param>
      /// <param name="action">A delegate that takes the <b>Transaction</b> as an argument.</param>
      public static void UsingTransaction(this Document document, Action<Transaction> action)
      {
         using (document.LockDocument())
         {
            document.Database.UsingTransaction(action);
         }
      }

	}

    /// <summary>
    ///   Extensions to the Database class.
    /// </summary>
    public static class MiscExtensions
    {

    }

   /// <summary>
   ///   Extensions to the default string class.
   /// </summary>
   public static class StringExtensions
   {
       /// <summary>
       /// Validates the name and path of the file.
       /// </summary>
       /// <param name="fileName">Name of the file.</param>
       /// <param name="filePath">Path of the file.</param>
       /// <returns><c>true</c> if the file is valid, <c>false</c> otherwise.</returns>
       private static bool IsValidFileNameWithPath(this string fileName, string filePath)
       {
           return (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) &&
               (filePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0) &&
               !File.Exists(Path.Combine(filePath, fileName));
       }
 
       /// <summary>
       /// Validates the name of the file.
       /// </summary>
       /// <param name="fileName">Name of the file.</param>
       /// <returns><c>true</c> if the file name is valid, <c>false</c> otherwise.</returns>
       private static bool IsValidFileName(this string fileName)
       {
           return (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) &&
               !File.Exists(Path.Combine(fileName));
       }
 
       /// <summary>
       /// Validates the file path.
       /// </summary>
       /// <param name="filePath">The file path.</param>
       /// <returns><c>true</c> if the path path is valid, <c>false</c> otherwise.</returns>
       private static bool IsValidFilePath(this string filePath)
       {
           return (filePath.IndexOfAny(Path.GetInvalidPathChars()) >= 0) &&
               !File.Exists(Path.Combine(filePath));
       }
   }

    public static class GeometryExtensions
    {
        /// <summary>
        /// Converts a double representing an angle in radians to degrees
        /// </summary>
        /// <param name="radians">The angle in radians</param>
        /// <returns>The angle in degrees</returns>

        public static double ToDegrees(this double radians)
        {
            return ((radians / 3.1415926535897931) * 180.0);
        }

        /// <summary>
        /// Converts a double representing an angle in degrees to radians
        /// </summary>
        /// <param name="degrees">The angle in degrees</param>
        /// <returns>The angle in radians</returns>

        public static double ToRadians(this double degrees)
        {
            return ((degrees / 180.0) * 3.1415926535897931);
        }

        /// <summary>
        /// 2D Polar coordinate specificiation (basepoint/angle/distance)
        /// </summary>
        /// <param name="basepoint">The basepoint from which the resulting point is computed</param>
        /// <param name="AngleInXYPlane">The direction (in radians) to the resulting point</param>
        /// <param name="distance">The distance to the resulting point</param>
        /// <returns>The point at the given distance and direction from the base point</returns>

        public static Point3d To(this Point3d basepoint, double angleInXYPlane, double distance)
        {
            return new Point3d(
               basepoint.X + (distance * Math.Cos(angleInXYPlane)),
               basepoint.Y + (distance * Math.Sin(angleInXYPlane)),
               basepoint.Z);
        }

        public static Point3d Convert3d(this Point2d pt) =>
            new Point3d(pt.X, pt.Y, 0.0);

        public static Matrix3d DCS2WCS(this Viewport vp) =>
            Matrix3d.Rotation(-vp.TwistAngle, vp.ViewDirection, vp.ViewTarget) *
            Matrix3d.Displacement(vp.ViewTarget.GetAsVector()) *
            Matrix3d.PlaneToWorld(vp.ViewDirection);

        public static Matrix3d WCS2DCS(this Viewport vp) =>
            Matrix3d.WorldToPlane(vp.ViewDirection) *
            Matrix3d.Displacement(vp.ViewTarget.GetAsVector().Negate()) *
            Matrix3d.Rotation(vp.TwistAngle, vp.ViewDirection, vp.ViewTarget);

        public static Matrix3d DCS2PSDCS(this Viewport vp) =>
            Matrix3d.Scaling(vp.CustomScale, vp.CenterPoint) *
            Matrix3d.Displacement(vp.ViewCenter.Convert3d().GetVectorTo(vp.CenterPoint));

        public static Matrix3d PSDCS2DCS(this Viewport vp) =>
            Matrix3d.Displacement(vp.CenterPoint.GetVectorTo(vp.ViewCenter.Convert3d())) *
            Matrix3d.Scaling(1.0 / vp.CustomScale, vp.CenterPoint);


    }

} 