using System;
using System.IO;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Threading.Tasks;
using System.Reflection;

namespace MNML
{
    public class GhASCParserComponent : GH_TaskCapableComponent<GhASCParserComponent.SolveResults>
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public GhASCParserComponent()
          : base("ASCParser", "ASC",
            "Parse ASC (ASCI GIS) data format",
            "MNML", "Parser")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Use the pManager object to register your input parameters.
            // You can often supply default values when creating parameters.
            // All parameters must have the correct access type. If you want 
            // to import lists or trees of values, modify the ParamAccess flag.
            pManager.AddTextParameter("File path", "F", "Filepath", GH_ParamAccess.item);

            // If you want to change properties of certain parameters, 
            // you can use the pManager instance to access them by index:
            //pManager[0].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            // Use the pManager object to register your output parameters.
            // Output parameters do not have default values, but they too must have the correct access type.
            pManager.AddNumberParameter("Columns", "C", "Number of columns", GH_ParamAccess.item);
            pManager.AddNumberParameter("Rows", "R", "Number of rows", GH_ParamAccess.item);
            pManager.AddPointParameter("Corner", "O", "Lower Left Corner", GH_ParamAccess.item);
            pManager.AddPointParameter("Center", "C", "Center", GH_ParamAccess.item);
            pManager.AddNumberParameter("CellSize", "S", "Cell Size", GH_ParamAccess.item);
            pManager.AddNumberParameter("Elevation", "E", "Elevation", GH_ParamAccess.list);

        }
        public class SolveResults
        {
            public Payload Value { get; set; }
        }

        private static SolveResults Parse(string path)
        {
            SolveResults result = new SolveResults();
            StreamReader sr = new StreamReader(path);
            Payload payload = new Payload();
            string line = "";
            int lineNumber = 0;
            char[] separator = { ' ' };
            List<float> values = new List<float>();
            while (line != null)
            {
                line = sr.ReadLine();
                if (line != null)
                {
                    if (lineNumber < 6)
                    {
                        var array = line.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                        var key = array[0];
                        var value = float.Parse(array[1]);
                        switch (key)
                        {
                            case "ncols": payload.ncols = (int)value; break; case "nrows": payload.nrows = (int)value; break;
                            case "xllcorner": payload.xllcorner = value; break; case "yllcorner": payload.yllcorner = value; break;
                            case "xllcenter": payload.xllcenter = value; break; case "yllcenter": payload.yllcenter = value; break;
                            case "cellsize": payload.cellsize = value; break;
                            case "nodata": payload.nodata = value; break;
                            default: break;
                        }
                    }
                    else
                    {
                        values.AddRange(Array.ConvertAll<string, float>(line.Split(separator, StringSplitOptions.RemoveEmptyEntries), s => float.Parse(s)));
                    }
                    lineNumber++;
                }
            }
            sr.Close();
            payload.values = values;
            result.Value = payload;
            return result;
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="data">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess data)
        {
            string filepath = "";

            
            if (InPreSolve)
            {
                // First pass; collect data and construct tasks
                if (!data.GetData(0, ref filepath)) return;

                // Run the task
                //Task<SolveResults> task = Task.Run(() => Parse(filepath), CancelToken);
                Task<SolveResults> task = Task.Run(() => Parse(filepath));
                TaskList.Add(task);
                return;
            }


            if (!GetSolveResults(data, out SolveResults result))
            {
                if (!data.GetData(0, ref filepath)) return;

                // 2. Compute
                result = Parse(filepath);
            }

            // 3. Set
            if (result != null)
            {
                var v = result.Value;
                data.SetData(0, v.ncols);
                data.SetData(1, v.nrows);
                data.SetData(2, new Point2d(v.xllcorner, v.yllcorner));
                data.SetData(3, new Point2d(v.xllcenter, v.yllcenter));
                data.SetData(4, v.cellsize);
                data.SetDataList(5, v.values);
            }

        }

 

        /// <summary>
        /// The Exposure property controls where in the panel a component icon 
        /// will appear. There are seven possible locations (primary to septenary), 
        /// each of which can be combined with the GH_Exposure.obscure flag, which 
        /// ensures the component will only be visible on panel dropdowns.
        /// </summary>
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("805c7dc3-8434-40f6-b363-7ff3533b2c19"); }
        }
    }
    
    public struct Payload
    {
        public int ncols;
        public int nrows;
        public float xllcorner;
        public float yllcorner;
        public float xllcenter;
        public float yllcenter;
        public float cellsize;
        public float nodata;

        public List<float> values;

        public object this[string name]
        {
            get
            {
                var properties = typeof(Payload)
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    if (property.Name == name && property.CanRead)
                        return property.GetValue(this, null);
                }
                return "";
                //throw new ArgumentException("Can't find property");

            }
            set
            {
                var properties = typeof(Payload)
                       .GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    if (property.Name == name && property.CanWrite)
                    {
                        property.SetValue(this, value);
                    }
                }
            }
        }
    };
}
