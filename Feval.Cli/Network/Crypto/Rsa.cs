using Helper;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Xml;


namespace Crypto
{
    public class Rsa
    {
        #region RSA 加密解密
        #region RSA 的密钥产生
        /// <summary>
        /// RSA产生密钥
        /// </summary>
        /// <param name="xmlKeys">私钥</param>
        /// <param name="xmlPublicKey">公钥</param>
        public static void Key(out string xmlKeys, out string xmlPublicKey)
        {
            try
            {
                var rsa = new RSACryptoServiceProvider();
                xmlKeys = rsa.toXmlString(true);
                xmlPublicKey = rsa.toXmlString(false);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region RSA加密函数
        //############################################################################## 
        //RSA 方式加密 
        //KEY必须是XML的形式,返回的是字符串 
        //该加密方式有长度限制的！
        //############################################################################## 

        /// <summary>
        /// RSA的加密函数
        /// </summary>
        /// <param name="xmlPublicKey">公钥</param>
        /// <param name="encryptString">待加密的字符串</param>
        /// <returns></returns>
        public static bool Encrypt(string xmlPublicKey, string encryptString, out string result)
        {
            try
            {
                byte[] PlainTextBArray;
                byte[] CypherTextBArray;
                var rsa = new RSACryptoServiceProvider();
                rsa.fromXmlString(xmlPublicKey);
                PlainTextBArray = (new UnicodeEncoding()).GetBytes(encryptString);
                CypherTextBArray = rsa.Encrypt(PlainTextBArray, true);
                result = Convert.ToBase64String(CypherTextBArray);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            result = string.Empty;
            return false;
        }
        /// <summary>
        /// RSA的加密函数 
        /// </summary>
        /// <param name="xmlPublicKey">公钥</param>
        /// <param name="EncryptString">待加密的字节数组</param>
        /// <returns></returns>
        public static bool Encrypt(string xmlPublicKey, byte[] EncryptString, out byte[] result)
        {
            try
            {
                var rsa = new RSACryptoServiceProvider();
                rsa.fromXmlString(xmlPublicKey);
                result = rsa.Encrypt(EncryptString, true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            result = null;
            return false;
        }
        #endregion

        #region RSA的解密函数        
        /// <summary>
        /// RSA的解密函数
        /// </summary>
        /// <param name="xmlPrivateKey">私钥</param>
        /// <param name="decryptString">待解密的字符串</param>
        /// <returns></returns>
        public static bool Decrypt(string xmlPrivateKey, string decryptString,out string result)
        {
            try
            {
                byte[] PlainTextBArray;
                byte[] DypherTextBArray;
                var rsa = new RSACryptoServiceProvider();
                rsa.fromXmlString(xmlPrivateKey);
                PlainTextBArray = Convert.FromBase64String(decryptString);
                DypherTextBArray = rsa.Decrypt(PlainTextBArray, true);
                result = (new UnicodeEncoding()).GetString(DypherTextBArray);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            result = string.Empty;
            return false;
        }
        /// <summary>
        /// RSA的解密函数 
        /// </summary>
        /// <param name="xmlPrivateKey">私钥</param>
        /// <param name="DecryptString">待解密的字节数组</param>
        /// <returns></returns>
        public static bool Decrypt(string xmlPrivateKey, byte[] DecryptString, out byte[] result)
        {
            try
            {
                var rsa = new RSACryptoServiceProvider();
                rsa.fromXmlString(xmlPrivateKey);
                result = rsa.Decrypt(DecryptString, true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            result = null;
            return false;
        }
        #endregion
        #region Check
        public static bool CheckIsPub(string xml)
        {
            RSAParameters parameters = new RSAParameters();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
            {
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "Modulus": parameters.Modulus = Convert.FromBase64String(node.InnerText); break;
                        case "Exponent": parameters.Exponent = Convert.FromBase64String(node.InnerText); break;
                        case "P": parameters.P = Convert.FromBase64String(node.InnerText); break;
                        case "Q": parameters.Q = Convert.FromBase64String(node.InnerText); break;
                        case "DP": parameters.DP = Convert.FromBase64String(node.InnerText); break;
                        case "DQ": parameters.DQ = Convert.FromBase64String(node.InnerText); break;
                        case "InverseQ": parameters.InverseQ = Convert.FromBase64String(node.InnerText); break;
                        case "D": parameters.D = Convert.FromBase64String(node.InnerText); break;
                    }
                }
                if (parameters.Modulus == null
                    || parameters.Exponent == null
                    || parameters.P != null
                    || parameters.Q != null
                    || parameters.DP != null
                    || parameters.DQ != null
                    || parameters.InverseQ != null
                    || parameters.D != null
                )
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }
        public static bool CheckIsKey(string xml)
        {
            RSAParameters parameters = new RSAParameters();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);
            if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
            {
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "Modulus": parameters.Modulus = Convert.FromBase64String(node.InnerText); break;
                        case "Exponent": parameters.Exponent = Convert.FromBase64String(node.InnerText); break;
                        case "P": parameters.P = Convert.FromBase64String(node.InnerText); break;
                        case "Q": parameters.Q = Convert.FromBase64String(node.InnerText); break;
                        case "DP": parameters.DP = Convert.FromBase64String(node.InnerText); break;
                        case "DQ": parameters.DQ = Convert.FromBase64String(node.InnerText); break;
                        case "InverseQ": parameters.InverseQ = Convert.FromBase64String(node.InnerText); break;
                        case "D": parameters.D = Convert.FromBase64String(node.InnerText); break;
                    }
                }
                if (parameters.Modulus == null
                    || parameters.Exponent == null
                    || parameters.P == null
                    || parameters.Q == null
                    || parameters.DP == null
                    || parameters.DQ == null
                    || parameters.InverseQ == null
                    || parameters.D == null
                )
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }
        #endregion
        #endregion
    }

    public static class RsaExtention
    {
        public static void fromXmlString(this RSACryptoServiceProvider rsa, string xmlString)
        {
            RSAParameters parameters = new RSAParameters();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);
            if (xmlDoc.DocumentElement.Name.Equals("RSAKeyValue"))
            {
                foreach (XmlNode node in xmlDoc.DocumentElement.ChildNodes)
                {
                    switch (node.Name)
                    {
                        case "Modulus": parameters.Modulus = Convert.FromBase64String(node.InnerText); break;
                        case "Exponent": parameters.Exponent = Convert.FromBase64String(node.InnerText); break;
                        case "P": parameters.P = Convert.FromBase64String(node.InnerText); break;
                        case "Q": parameters.Q = Convert.FromBase64String(node.InnerText); break;
                        case "DP": parameters.DP = Convert.FromBase64String(node.InnerText); break;
                        case "DQ": parameters.DQ = Convert.FromBase64String(node.InnerText); break;
                        case "InverseQ": parameters.InverseQ = Convert.FromBase64String(node.InnerText); break;
                        case "D": parameters.D = Convert.FromBase64String(node.InnerText); break;
                    }
                }
            }
            else
            {
                throw new Exception("Invalid XML RSA key.");
            }

            rsa.ImportParameters(parameters);
        }

        public static string toXmlString(this RSACryptoServiceProvider rsa, bool includePrivateParameters)
        {
            RSAParameters parameters = rsa.ExportParameters(includePrivateParameters);

            if (includePrivateParameters)
            {
                return string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent><P>{2}</P><Q>{3}</Q><DP>{4}</DP><DQ>{5}</DQ><InverseQ>{6}</InverseQ><D>{7}</D></RSAKeyValue>",
                    Convert.ToBase64String(parameters.Modulus),
                    Convert.ToBase64String(parameters.Exponent),
                    Convert.ToBase64String(parameters.P),
                    Convert.ToBase64String(parameters.Q),
                    Convert.ToBase64String(parameters.DP),
                    Convert.ToBase64String(parameters.DQ),
                    Convert.ToBase64String(parameters.InverseQ),
                    Convert.ToBase64String(parameters.D));
            }
            return string.Format("<RSAKeyValue><Modulus>{0}</Modulus><Exponent>{1}</Exponent></RSAKeyValue>",
                    Convert.ToBase64String(parameters.Modulus),
                    Convert.ToBase64String(parameters.Exponent));
        }

    }
}