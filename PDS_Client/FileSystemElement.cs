using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Drawing;
using System.Windows.Controls;

namespace PDS_Client
{
    public class FileSystemElement 
    {
        private Bitmap icon;
        String pname; // private name

        public Bitmap image {
            get {
                return icon;
            }
            set
            {
                icon = value;
            }
        }

        public String name
        {
            get
            {
                return pname;
            }
            set
            {
                pname = value;
            }
        }
        
        

        public FileSystemElement(Bitmap image, String name)
        {
            this.image = image;
            this.pname = name;
        }

        
        
    }
}
