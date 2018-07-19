using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Certificados.Models
{
    public class Participante
    {
        public string email { get; set; }
        public byte[] certificado { get; set; }
    }
}