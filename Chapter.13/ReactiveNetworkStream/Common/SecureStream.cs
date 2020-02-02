using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Common
{
    public static class SecureStream
    {
        public static Stream GetServerStream(TcpClient client, string nameSsl = null)
        {
            if (string.IsNullOrWhiteSpace(nameSsl))
                return client.GetStream();

            X509Certificate getSslCertificate()
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
                {
                    store.Open(OpenFlags.ReadOnly);
                    X509CertificateCollection certificate =
                        store.Certificates.Find(X509FindType.FindBySubjectName, nameSsl, true);
                    return certificate.Count > 0 ? certificate[0] : null;
                }
            }

            var sslStream = new SslStream(client.GetStream());
            sslStream.AuthenticateAsServer(getSslCertificate(), false, SslProtocols.Default, true);
            return sslStream;
        }

        public static Stream GetClientStream(TcpClient client, string nameSsl = null)
        {
            if (string.IsNullOrWhiteSpace(nameSsl))
                return client.GetStream();

            var sslStream = new SslStream(client.GetStream(), false,
                CertificateValidationCallback);
            sslStream.AuthenticateAsClient(nameSsl);

            return sslStream;
        }

        private static bool CertificateValidationCallback(object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            return sslPolicyErrors == SslPolicyErrors.None;
        }
    }
}