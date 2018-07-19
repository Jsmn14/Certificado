using iTextSharp.text;
using iTextSharp.text.pdf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Net;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;
using System.Threading;
using Certificados.Models;
using System.Text.RegularExpressions;

namespace Certificados.Controllers
{
    public class CertificadoController : Controller
    {
        // GET: Certificados
        public ActionResult Index()
        {
            return View();
        }

        public static byte[] CriarCertificado(string conteudo, byte[] planoFundo, bool textoNegrito, bool textoItalico, int tamanhoFonte)
        {
            
            using (System.IO.MemoryStream memoryStream = new System.IO.MemoryStream())
            {
                iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(planoFundo);

                var document = new Document(PageSize.A4.Rotate());
                document.SetMargins(60, 80, 60, 70);
                PdfWriter writer = PdfWriter.GetInstance(document, memoryStream);
                document.Open();

                BaseFont bf = BaseFont.CreateFont(BaseFont.TIMES_ROMAN, BaseFont.CP1252, BaseFont.NOT_EMBEDDED);
                iTextSharp.text.Font font = null;

                if (!textoNegrito && !textoItalico)
                {
                    font = new iTextSharp.text.Font(bf, tamanhoFonte - 10, iTextSharp.text.Font.NORMAL);
                }else
                {
                    if(textoNegrito && textoItalico)
                    {
                        font = new iTextSharp.text.Font(bf, tamanhoFonte - 10, iTextSharp.text.Font.BOLDITALIC);
                    }
                    else if (!textoNegrito && textoItalico)
                    {
                        font = new iTextSharp.text.Font(bf, tamanhoFonte - 10, iTextSharp.text.Font.ITALIC);
                    }else
                    {
                        font = new iTextSharp.text.Font(bf, tamanhoFonte - 10, iTextSharp.text.Font.BOLD);
                    }
                }
                
                Paragraph texto = new Paragraph(new Chunk(conteudo, font));
                texto.Alignment = 1;
     
                document.Add(texto);

                image.Alignment = iTextSharp.text.Image.UNDERLYING;
                float larguraPagina = document.PageSize.Width;
                float alturaPagina = document.PageSize.Height;
                image.ScaleToFit(larguraPagina, alturaPagina);
                image.SetAbsolutePosition(0, 0);
                document.Add(image);

                document.Close();

                var pdf = memoryStream.ToArray();
                memoryStream.Close();

                return pdf;
            }

        }
        
        public async Task<ActionResult> Enviar()
        {
            var cabecalhos = new List<string>();
            var listaEmail = new List<Participante>();

            if (System.Web.HttpContext.Current.Request.Files.AllKeys.Any())
            {
                byte[] fileData = null;

                var imgPlanoFundo = System.Web.HttpContext.Current.Request.Files["planoFundo"];
                var participantes = System.Web.HttpContext.Current.Request.Params["participantes"];
                var conteudoCertificado = System.Web.HttpContext.Current.Request.Params["conteudoCertificado"];
                var textoNegrito = System.Web.HttpContext.Current.Request.Params["negrito"] == "true" ? true: false;
                var textoItalico = System.Web.HttpContext.Current.Request.Params["italico"] == "true" ? true : false;
                var tamanhoFonte = Convert.ToInt32(System.Web.HttpContext.Current.Request.Params["tamanhoFonte"]);

                if (imgPlanoFundo != null && imgPlanoFundo.InputStream.Length > 0)
                {
                    using (var binaryReader = new BinaryReader(imgPlanoFundo.InputStream))
                    {
                        fileData = binaryReader.ReadBytes(imgPlanoFundo.ContentLength);
                    }

                }

                using (StringReader reader = new StringReader(participantes))
                {
                    int count = 0;
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if(line != "")
                        {
                            //cabecallho
                            if (count == 0)
                            {
                                cabecalhos = line.Split('\t').ToList();
                                var retorno = VerificarTagsCertificado(cabecalhos, conteudoCertificado);

                                if (!retorno.Item1)
                                {
                                    return Json(new { status = "error", message = retorno.Item2 });
                                }
                            }
                            else
                            {
                                var dadosParticipante = line.Split('\t').ToList();

                                string conteudoParticipante = conteudoCertificado;

                                string emailParticipante = null;

                                for (var i = 0; i < cabecalhos.Count(); i++)
                                {
                                    if (cabecalhos[i].ToLower() == "email")
                                    {
                                        emailParticipante = dadosParticipante[i];
                                    }

                                    conteudoParticipante = conteudoParticipante.Replace("{{" + cabecalhos[i].ToLower() + "}}", dadosParticipante[i]);
                                }

                                var pdf = CriarCertificado(conteudoParticipante, fileData, textoNegrito, textoItalico, tamanhoFonte);

                                var participanteEmail = new Participante
                                {
                                    email = emailParticipante,
                                    certificado = pdf
                                };

                                listaEmail.Add(participanteEmail);

                            }
                            count++;
                        }
                    }

                    EnviarEmail(listaEmail);
                    System.IO.File.WriteAllBytes(@"C:\Users\jully\Desktop\teste.pdf", listaEmail[0].certificado);

                }

                return Json(new { status = "success", message = "Cerfificados enviados com sucesso" });
            }

            return Json(new { status = "error", message = "Insira um plano de fundo para o seu certificado." });
        }

        public Tuple<bool, string> VerificarTagsCertificado(List<string> certificados, string conteudo)
        {
            var quantidadeTagsInicio = Regex.Matches(conteudo, "{{").Count;
            var quantidadeTagsFinal = Regex.Matches(conteudo, "}}").Count;

            if(quantidadeTagsFinal != quantidadeTagsInicio)
            {
                return Tuple.Create<bool, string>(false, "O formato das tags informadas no certificado está incorreto.");
            }

            for (var i = 0; i < quantidadeTagsInicio; i++)
            {
                int inicioTag = conteudo.IndexOf("{{");
                int finalTag = conteudo.IndexOf("}}");
                var tamanhoTag = finalTag -inicioTag - 2;
                var tagCertificado = conteudo.Substring(inicioTag + 2, tamanhoTag);
                var tagExistente = certificados.Exists(c => c.ToLower() == tagCertificado);

                if (!tagExistente)
                {
                    return Tuple.Create<bool, string>(false, "Não foi identificado o cabeçalho com o tag: " + tagCertificado);
                }

                conteudo = conteudo.Substring(finalTag+2);
            }

            return Tuple.Create<bool, string>(true, null);
           
        }

        public void EnviarEmail(List<Participante> participantes)
        {
            Task.Run(async () =>
            {
                Thread.Sleep(10000);

                foreach (var participante in participantes)
                {
                    var client = new SendGridClient(System.Configuration.ConfigurationManager.AppSettings["apikeySendGrid"].ToString());
                    var from = new EmailAddress("teste@teste.com");
                    var subject = "Certificado";
                    var to = new EmailAddress(participante.email);
                    var htmlContent = "<p>O certificado está anexado ao email.</p>";
                    var msg = MailHelper.CreateSingleEmail(from, to, subject, null, htmlContent);
                    string stringBase64 = Convert.ToBase64String(participante.certificado);
                    msg.AddAttachment("certificado.pdf", stringBase64);
                    try
                    {
                        var response = await client.SendEmailAsync(msg);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(ex.Message);
                    }

                }

                return true;
            });
        }
    }
}