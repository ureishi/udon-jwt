using System;
using UnityEngine;
using UdonSharp;
using Koyashiro.UdonJson;
using Koyashiro.UdonEncoding;
using Koyashiro.UdonJwt.Verifier;

namespace Koyashiro.UdonJwt
{
    public class JwtDecoder : UdonSharpBehaviour
    {
        [SerializeField]
        private JwtAlgorithmKind _algorithmKind;

        #region RS256
        [SerializeField]
        private RS256Verifier _rs256Verifier;

        [SerializeField, TextArea(10, 20)]
        private string _publicKey;

        [SerializeField, HideInInspector]
        private int _e;

        [SerializeField, HideInInspector]
        private uint[] _n;

        [SerializeField, HideInInspector]
        private uint[] _nInverse;

        [SerializeField, HideInInspector]
        private int _fixedPointLength;
        #endregion

        private UdonSharpBehaviour _callbackThis;
        private string _callbackEventName;
        private bool _result;
        private UdonJsonValue _header;
        private UdonJsonValue _payload;

        public JwtAlgorithmKind AlgorithmKind => _algorithmKind;

        public RS256Verifier RS256Verifier => _rs256Verifier;

        public string PublicKey => _publicKey;

        public int E => _e;

        public uint[] N => _n;

        public uint[] NInverse => _nInverse;

        public int FixedPointLength => _fixedPointLength;

        public bool Result => _result;

        public UdonJsonValue Header => _header;

        public UdonJsonValue Payload => _payload;

        public void SetPublicKey(int e, uint[] n, uint[] nInverse, int fixedPointLength)
        {
            _e = e;
            _n = n;
            _nInverse = nInverse;
            _fixedPointLength = fixedPointLength;
            _rs256Verifier.SetPublicKey(e, n, nInverse, fixedPointLength);
        }

        public void Decode(string token, UdonSharpBehaviour callbackThis, string callbackEventName)
        {
            if (token == null)
            {
                callbackThis.SendCustomEventDelayedFrames(callbackEventName, 1);
                return;
            }

            var splitTokens = token.Split('.');
            if (splitTokens.Length != 3)
            {
                callbackThis.SendCustomEventDelayedFrames(callbackEventName, 1);
                return;
            }

            var headerBase64 = ToBase64(splitTokens[0]);
            var payloadBase64 = ToBase64(splitTokens[1]);
            var signatureBase64 = ToBase64(splitTokens[2]);

            if (!UdonJsonDeserializer.TryDeserialize(UdonUTF8.GetString(Convert.FromBase64String(headerBase64)), out var header))
            {
                callbackThis.SendCustomEventDelayedFrames(callbackEventName, 1);
                return;
            }

            if (header.GetKind() != UdonJsonValueKind.Object)
            {
                callbackThis.SendCustomEventDelayedFrames(callbackEventName, 1);
                return;
            }

            // TODO: check header

            if (!UdonJsonDeserializer.TryDeserialize(UdonUTF8.GetString(Convert.FromBase64String(payloadBase64)), out var payload))
            {
                callbackThis.SendCustomEventDelayedFrames(callbackEventName, 1);
                return;
            }

            // TODO: check body

            var signature = Convert.FromBase64String(signatureBase64);

            _header = header;
            _payload = payload;
            _callbackThis = callbackThis;
            _callbackEventName = callbackEventName;

            switch (_algorithmKind)
            {
                case JwtAlgorithmKind.RS256:
                    _rs256Verifier.Verify(headerBase64, payloadBase64, signature, this, nameof(_Decode));
                    break;
            }
        }

        public void _Decode()
        {
            _result = _rs256Verifier.Result;
            _callbackThis.SendCustomEventDelayedFrames(_callbackEventName, 1);
        }

        public static string ToBase64(string base64Url)
        {
            var base64 = base64Url.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 1:
                    base64 += "===";
                    break;
                case 2:
                    base64 += "==";
                    break;
                case 3:
                    base64 += "=";
                    break;
            }
            return base64;
        }
    }
}
