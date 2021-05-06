using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Common;
using Xbim.Common.Enumerations;
using Xbim.Common.ExpressValidation;
using Xbim.Common.Step21;
using Xbim.Ifc;
using Xbim.Ifc.Validation;
using Xbim.Ifc4.GeometricConstraintResource;
using Xbim.Ifc4.GeometricModelResource;
using Xbim.Ifc4.GeometryResource;
using Xbim.Ifc4.Interfaces;
using Xbim.Ifc4.Kernel;
using Xbim.Ifc4.MaterialResource;
using Xbim.Ifc4.ProductExtension;
using Xbim.Ifc4.ProfileResource;
using Xbim.Ifc4.RepresentationResource;
using Xbim.Ifc4.SharedBldgElements;
using Xbim.IO;

// Example Xbim usage here: https://github.com/xBimTeam/XbimSamples/blob/master/HelloWall/HelloWallExample.cs

namespace AutoCADPluginExample
{
    /// <summary>
    /// This singleton represents the IFC model that we are currently editing in the AutoCAD plugin.
    /// The model contains all information from the AutoCAD drawing that will be transformed into the BIM/IFC model.
    /// 
    /// Based on: https://refactoring.guru/design-patterns/singleton/csharp/example#example-1
    /// This Singleton implementation is called "double check lock". It is safe
    /// in multithreaded environment and provides lazy initialization for the
    /// Singleton object.
    /// </summary>
    class IfcModelSingleton
    {
        public IfcStore model { get; set; }
        public string projectName { get; set; }
        private string fileName { get; set; }

        private IfcModelSingleton(string projectName)
        {
            this.projectName = projectName;
            this.fileName = projectName + ".ifc";
            this.model = CreateandInitModel(CreateDefaultCredentials(), this.projectName);

            // Add building and storey
            var project = model.Instances.OfType<IfcProject>().FirstOrDefault();
            var building = CreateBuilding(model, "Default building", project);
            var storey = CreateBuildingStorey(model, "Default storey", 0.0, building);
        }

        private static IfcModelSingleton _instance;

        // We now have a lock object that will be used to synchronize threads
        // during first access to the Singleton.
        private static readonly object _lock = new object();

        public static IfcModelSingleton GetInstance()
        {
            // This conditional is needed to prevent threads stumbling over the
            // lock once the instance is ready.
            if (_instance == null)
            {
                // Now, imagine that the program has just been launched. Since
                // there's no Singleton instance yet, multiple threads can
                // simultaneously pass the previous conditional and reach this
                // point almost at the same time. The first of them will acquire
                // lock and will proceed further, while the rest will wait here.
                lock (_lock)
                {
                    // The first thread to acquire the lock, reaches this
                    // conditional, goes inside and creates the Singleton
                    // instance. Once it leaves the lock block, a thread that
                    // might have been waiting for the lock release may then
                    // enter this section. But since the Singleton field is
                    // already initialized, the thread won't create a new
                    // object.
                    if (_instance == null)
                    {
                        _instance = new IfcModelSingleton("TestProject");
                    }
                }
            }
            return _instance;
        }

        public void ResetModel()
        {
        }

        public void ValidateAndSave()
        {
            this.model.SaveAs(this.fileName, StorageType.Ifc);

            if (IsModelValid(this.model))
            {
                // TODO
            }
            else
            {
                // throw new Exception("IFC model syntax is not valid");
            }
        }

        /// <summary>
        /// Sets up the basic parameters any model must provide, units, ownership etc
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <returns></returns>
        public static IfcStore CreateandInitModel(XbimEditorCredentials credentials, string projectName)
        {

            // Now we can create an IfcStore, it is in Ifc4 format and will be held in memory rather than in a database
            // Database is normally better in performance terms if the model is large >50MB of Ifc or if robust transactions are required
            var model = IfcStore.Create(credentials, XbimSchemaVersion.Ifc4, XbimStoreType.InMemoryModel);
            // The following line is needed to avoid a Header error
            model.Header.FileDescription.Description.Add("ViewDefinition [CoordinationView]");

            //Begin a transaction as all changes to a model are ACID
            using (var txn = model.BeginTransaction("Initialise Model"))
            {
                //create a project
                var project = model.Instances.New<IfcProject>();
                //set the units to SI (mm and metres)
                project.Initialize(ProjectUnits.SIUnitsUK);
                project.Name = projectName;
                //now commit the changes, else they will be rolled back at the end of the scope of the using statement
                txn.Commit();
            }
            return model;
        }

        public static IfcBuilding CreateBuilding(IfcStore model, string name, IfcProject project)
        {
            using (var txn = model.BeginTransaction("Create Building"))
            {
                var building = model.Instances.New<IfcBuilding>(b =>
                {
                    b.Name = name;
                    b.CompositionType = IfcElementCompositionEnum.ELEMENT;
                });

                var localPlacement = model.Instances.New<IfcLocalPlacement>();
                building.ObjectPlacement = localPlacement;
                var placement = model.Instances.New<IfcAxis2Placement3D>();
                localPlacement.RelativePlacement = placement;
                placement.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
                //get the project there should only be one and it should exist
                project.AddBuilding(building);

                txn.Commit();
                return building;
            }
        }

        public static IfcBuildingStorey CreateBuildingStorey(IfcStore model, string name, double elevation, IfcBuilding building)
        {
            using (var txn = model.BeginTransaction("Create Building Storey"))
            {
                var storey = model.Instances.New<IfcBuildingStorey>(s =>
                {
                    s.Name = name;
                    s.Elevation = elevation;
                });

                building.AddToSpatialDecomposition(storey);

                txn.Commit();
                return storey;
            }
        }

        public static IfcWallStandardCase CreateWall(IfcStore model, double posX, double posY, double dirX, double dirY, double dirZ, double length, double width, double height, IfcBuildingStorey storey)
        {
            using (var txn = model.BeginTransaction("Create Wall"))
            {
                // Point to insert the wall (on the 2D space)
                var insertPoint = model.Instances.New<IfcCartesianPoint>(p =>
                {
                    p.SetXY(0, 0);
                });

                // Create rectangular profile for wall
                var rectProf = model.Instances.New<IfcRectangleProfileDef>(r =>
                {
                    r.ProfileType = IfcProfileTypeEnum.AREA;
                    r.XDim = length;
                    r.YDim = width;
                    r.Position = model.Instances.New<IfcAxis2Placement2D>();
                    r.Position.Location = insertPoint;
                });

                // Point to insert the geometry in the model
                var origin = model.Instances.New<IfcCartesianPoint>(o =>
                {
                    o.SetXYZ(0, 0, 0);
                });

                var locationPoint = model.Instances.New<IfcCartesianPoint>(p =>
                {
                    p.SetXYZ(posX, posY, 0);
                });

                // Create an extruded area solid.
                // This type of solid is defined by sweeping a planar surface by some direction and depth
                // Here, we define first the profile of the bottom wall face and "build" the rest of the wall by sweeping it up
                var body = model.Instances.New<IfcExtrudedAreaSolid>(b =>
                {
                    b.Depth = height;
                    b.SweptArea = rectProf;
                    b.ExtrudedDirection = model.Instances.New<IfcDirection>();
                    b.ExtrudedDirection.SetXYZ(0, 0, 1);
                    b.Position = model.Instances.New<IfcAxis2Placement3D>();
                    b.Position.Location = origin;
                });


                // Create a Definition shape to hold the geometry
                var modelContext = model.Instances.OfType<IfcGeometricRepresentationContext>().FirstOrDefault();
                var shape = model.Instances.New<IfcShapeRepresentation>(s =>
                {
                    s.ContextOfItems = modelContext;
                    s.RepresentationType = "SweptSolid";
                    s.RepresentationIdentifier = "Body";
                    s.Items.Add(body);
                });

                // Create a Product Definition and add the model geometry to the wall
                var rep = model.Instances.New<IfcProductDefinitionShape>(r =>
                {
                    r.Representations.Add(shape);
                });

                // Create wall placement
                var ax3D = model.Instances.New<IfcAxis2Placement3D>(a =>
                {
                    a.Location = locationPoint;
                    a.RefDirection = model.Instances.New<IfcDirection>();
                    a.RefDirection.SetXYZ(dirX, dirY, 0);
                    a.Axis = model.Instances.New<IfcDirection>();
                    a.Axis.SetXYZ(0, 0, 1);
                });

                var lp = model.Instances.New<IfcLocalPlacement>(l =>
                {
                    l.RelativePlacement = ax3D;
                });

                // Create the wall and place it
                var wall = model.Instances.New<IfcWallStandardCase>(w =>
                {
                    w.Name = "A standard wall";
                    w.Representation = rep;
                    w.ObjectPlacement = lp;
                });

                // Where Clause: The IfcWallStandard relies on the provision of an IfcMaterialLayerSetUsage 
                var ifcMaterialLayerSetUsage = model.Instances.New<IfcMaterialLayerSetUsage>();
                var ifcMaterialLayerSet = model.Instances.New<IfcMaterialLayerSet>();
                var ifcMaterialLayer = model.Instances.New<IfcMaterialLayer>();
                ifcMaterialLayer.LayerThickness = 10;
                ifcMaterialLayerSet.MaterialLayers.Add(ifcMaterialLayer);
                ifcMaterialLayerSetUsage.ForLayerSet = ifcMaterialLayerSet;
                ifcMaterialLayerSetUsage.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS2;
                ifcMaterialLayerSetUsage.DirectionSense = IfcDirectionSenseEnum.NEGATIVE;
                ifcMaterialLayerSetUsage.OffsetFromReferenceLine = 150;

                // Add material to wall
                var material = model.Instances.New<IfcMaterial>();
                material.Name = "some material";
                var ifcRelAssociatesMaterial = model.Instances.New<IfcRelAssociatesMaterial>();
                ifcRelAssociatesMaterial.RelatingMaterial = material;
                ifcRelAssociatesMaterial.RelatedObjects.Add(wall);

                ifcRelAssociatesMaterial.RelatingMaterial = ifcMaterialLayerSetUsage;

                // Add wall to storey
                storey.AddElement(wall);
                txn.Commit();

                return wall;
            }
        }

        public static IfcSpace CreateSpace(IfcStore model, IfcBuildingStorey buildingStorey, IEnumerable<IfcWall> walls, string name, string description, string longName, IfcElementCompositionEnum compositionType)
        {
            // StackOverflow answer shedding some light on an IfcSpace representation:
            // https://stackoverflow.com/a/66103155
            using (var txn = model.BeginTransaction("Create Space: " + name))
            {
                var space = model.Instances.New<IfcSpace>(s =>
                {
                    // Space creation examples:
                    // https://github.com/xBimTeam/XbimExchange/blob/58b3dcc6174bf1dc15e7985ecb7b173efb85bf90/Xbim.COBie/Serialisers/XbimSerialiser/COBieXBimSpace.cs

                    s.Name = name;
                    s.Description = description;
                    s.LongName = longName;
                    s.CompositionType = compositionType;
                });

                List<IfcRelSpaceBoundary> boundaries = new List<IfcRelSpaceBoundary>();

                var enumerator = walls.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var currWall = enumerator.Current;

                    var relSpaceBoundary = model.Instances.New<IfcRelSpaceBoundary>(sb =>
                    {
                        sb.RelatedBuildingElement = currWall;
                        sb.RelatingSpace = space;
                    });

                    boundaries.Add(relSpaceBoundary);
                }

                buildingStorey.AddToSpatialDecomposition(space);
                txn.Commit();

                return space;
            }

        }

        /// <summary>
        /// Verifies if a model has valid IFC syntax.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static bool IsModelValid(Xbim.Common.IModel model)
        {
            var validator = new Validator()
            {
                CreateEntityHierarchy = true,
                ValidateLevel = ValidationFlags.All
            };

            var validationResult = validator.Validate(model);
            var ifcValidationReporter = new IfcValidationReporter(validationResult);

            if (ifcValidationReporter.Count() > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// The credentials for ownership of data in the model.
        /// </summary>
        /// <param name="developersName"></param>
        /// <param name="applicationName"></param>
        /// <param name="applicationId"></param>
        /// <param name="applicationVersion"></param>
        /// <param name="editorsFamilyName"></param>
        /// <param name="editorsGivenName"></param>
        /// <param name="editorsOrganisationName"></param>
        /// <returns></returns>
        public static XbimEditorCredentials CreateCredentials(string developersName, string applicationName, string applicationId, string applicationVersion, string editorsFamilyName, string editorsGivenName, string editorsOrganisationName)
        {
            //first we need to set up some credentials for ownership of data in the new model
            var credentials = new XbimEditorCredentials
            {
                ApplicationDevelopersName = developersName,
                ApplicationFullName = applicationName,
                ApplicationIdentifier = applicationId,
                ApplicationVersion = applicationVersion,
                EditorsFamilyName = editorsFamilyName,
                EditorsGivenName = editorsGivenName,
                EditorsOrganisationName = editorsOrganisationName
            };

            return credentials;
        }

        public static XbimEditorCredentials CreateDefaultCredentials()
        {
            return CreateCredentials("xbim developer", "app", "app.exe", "1.0", "team", "x", "y");
        }
    }
}
