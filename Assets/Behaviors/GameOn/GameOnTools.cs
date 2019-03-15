/*
 * Copyright 2019 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this
 * software and associated documentation files (the "Software"), to deal in the Software
 * without restriction, including without limitation the rights to use, copy, modify,
 * merge, publish, distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
 * PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using UnityEngine;

namespace Behaviors.GameOn
{
    public class GameOnTools : MonoBehaviour
    {
        public string gamePublicKey;

        public class KeyPair
        {
            public readonly string Private;
            public readonly string Public;


            private KeyPair(string publicKey = null, string privateKey = null)
            {
                Public = publicKey;
                Private = privateKey;
            }

//this functions generates a key pair using the bouncy castle algorithm 
            public static KeyPair Generate(short keySize = 1024)
            {
                var g = new RsaKeyPairGenerator();
                g.Init(new KeyGenerationParameters(new SecureRandom(), keySize));
                var pair = g.GenerateKeyPair();
                var privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(pair.Private);
                var serializedPrivateBytes = privateKeyInfo.ToAsn1Object().GetDerEncoded();
                var serializedPrivate = Convert.ToBase64String(serializedPrivateBytes);
                var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(pair.Public);
                var serializedPublicBytes = publicKeyInfo.ToAsn1Object().GetDerEncoded();
                var serializedPublic = Convert.ToBase64String(serializedPublicBytes);
                return new KeyPair(serializedPublic, serializedPrivate);
            }
        }

        public class Crypto
        {
            //this function encrypts a string using a public key
            public static string Encrypt(string publicKey, string payload)
            {
                var pubKey = (RsaKeyParameters) PublicKeyFactory.CreateKey(Convert.FromBase64String(publicKey));
                var pubParam = DotNetUtilities.ToRSAParameters(pubKey);
                var pubCsp = new RSACryptoServiceProvider();
                pubCsp.ImportParameters(pubParam);
                var encrypted = pubCsp.Encrypt(Encoding.UTF8.GetBytes(payload), false);
                return Convert.ToBase64String(encrypted);
            }

            //this function decrypts a string using a private key
            public static string Decrypt(string privateKey, string encryptedPayload)
            {
                var priKey =
                    (RsaPrivateCrtKeyParameters) PrivateKeyFactory.CreateKey(Convert.FromBase64String(privateKey));
                var priParam = DotNetUtilities.ToRSAParameters(priKey);
                var priCsp = new RSACryptoServiceProvider();
                priCsp.ImportParameters(priParam);
                var decrypted = priCsp.Decrypt(Convert.FromBase64String(encryptedPayload), false);
                return Encoding.UTF8.GetString(decrypted);
            }
        }
    }
}