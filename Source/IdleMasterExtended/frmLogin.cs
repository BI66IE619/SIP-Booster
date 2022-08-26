﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using SteamKit2;

namespace IdleMasterExtended
{
    public partial class frmLogin : Form
    {


        private SteamClient steamClient;
        private CallbackManager callbackManager;
        private SteamUser steamUser;

        private SteamConfiguration steamConfiguration;

        private string steamUsername;
        private string steamPassword;
        private string steamGuardCode;

        private SteamID steamUserID;
        private string steamUserNonce;
        private string steamVanityURL;
        private EUniverse steamClientUniverse;

        private uint steamLoginKeyUniqueID;

        private string steamUserLoginToken;
        private string steamUserLoginSecure;

        private bool isRunning;
        

        public frmLogin()
        {
            InitializeComponent();

            // Initialize the client and user objects to handle the login
            steamClient = new SteamClient();
            callbackManager = new CallbackManager(steamClient);
            steamUser = steamClient.GetHandler<SteamUser>();

            // Set up the callback manager for events we need
            callbackManager.Subscribe<SteamClient.ConnectedCallback>(OnConnected);
            callbackManager.Subscribe<SteamClient.DisconnectedCallback>(OnDisconnected);

            callbackManager.Subscribe<SteamUser.LoggedOnCallback>(OnLoggedOn);
            callbackManager.Subscribe<SteamUser.LoggedOffCallback>(OnLoggedOff);
            callbackManager.Subscribe<SteamUser.UpdateMachineAuthCallback>(OnMachineAuth);

            callbackManager.Subscribe<SteamUser.LoginKeyCallback>(OnLoginKeyReceived);
            callbackManager.Subscribe<SteamUser.SessionTokenCallback>(OnSessionTokenCallback);
            callbackManager.Subscribe<SteamUser.WebAPIUserNonceCallback>(OnWebAPIUserNonceCallback);
        }

        void OnConnected(SteamClient.ConnectedCallback callback)
        {
            // TODO: Sentry file?

            steamUser.LogOn(new SteamUser.LogOnDetails
            {
                Username = steamUsername,
                Password = steamPassword,
                AuthCode = steamGuardCode,
                TwoFactorCode = steamGuardCode,
            });
        }

        void OnDisconnected(SteamClient.DisconnectedCallback callback)
        {
            MessageBox.Show("Disconnected from Steam..." + callback.ToString());
            isRunning = false;
        }


        void OnLoggedOn(SteamUser.LoggedOnCallback callback)
        {

            if (callback.Result != EResult.OK)
            {
                MessageBox.Show("Unable to logon to Steam: " + callback.Result.ToString());

                isRunning = false;
                return;
            }

            MessageBox.Show("Logged on to Steam as user: " + steamUser.SteamID);

            steamUserID = steamUser.SteamID;
            
            steamClientUniverse = steamClient.Universe;
            
            steamUserNonce = callback.WebAPIUserNonce;
            steamVanityURL = callback.VanityURL;

        }

        void OnLoggedOff(SteamUser.LoggedOffCallback callback)
        {
            MessageBox.Show("Disconnected from Steam..." + callback.ToString());
            isRunning = false;
        }

        async void OnLoginKeyReceived(SteamUser.LoginKeyCallback callback)
        {
            steamLoginKeyUniqueID = callback.UniqueID;
            
            if (steamUserNonce is null)
            {
                var webApiUserNonceCallback = await steamUser.RequestWebAPIUserNonce();
                steamUserNonce = webApiUserNonceCallback.Nonce;
            }
            else
            {
                MessageBox.Show("We already have a user nonce");
            }

            byte[] publicKey = KeyDictionary.GetPublicKey(steamClientUniverse);
            RSACrypto rsa = new RSACrypto(publicKey);

            byte[] sessionKey = CryptoHelper.GenerateRandomBlock(32);
            byte[] encryptedSessionKey = rsa.Encrypt(sessionKey);

            byte[] loginKey = Encoding.UTF8.GetBytes(steamUserNonce);
            byte[] encryptedLoginKey = CryptoHelper.SymmetricEncrypt(loginKey, sessionKey);


            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments.Add("encrypted_loginkey", encryptedLoginKey);
            arguments.Add("sessionkey", encryptedSessionKey);
            arguments.Add("steamid", steamUserID);


            steamConfiguration = SteamConfiguration.Create(c => c.WithHttpClientFactory(() => new System.Net.Http.HttpClient()));

            WebAPI.AsyncInterface steamUserAuthService = steamConfiguration.GetAsyncWebAPIInterface("ISteamUserAuth");

            try
            {
                var responseWebAPI = await steamUserAuthService.CallAsync(HttpMethod.Post, "AuthenticateUser", args: arguments).ConfigureAwait(false);
                steamUserLoginToken = responseWebAPI["token"].AsString();
                steamUserLoginSecure = responseWebAPI["tokensecure"].AsString();
            } 
            catch
            {
                isRunning = false;
                MessageBox.Show("WARNING: Could not get a response...");
            }

            string sessionID = Convert.ToBase64String(Encoding.UTF8.GetBytes(steamUserID.ToString()));
        }

        private void OnWebAPIUserNonceCallback(SteamUser.WebAPIUserNonceCallback callback)
        {
            MessageBox.Show("I got my Web API Nonce callback! " + callback);
        }

        void OnSessionTokenCallback(SteamUser.SessionTokenCallback callback)
        {
            MessageBox.Show("I got my session token! " + callback.SessionToken);
        }

        void OnMachineAuth(SteamUser.UpdateMachineAuthCallback callback)
        {
            MessageBox.Show("Sentry file returned..." + callback.ToString());
        }

        private void buttonLogin_Click(object sender, EventArgs e)
        {
            buttonLogin.Enabled = false;

            steamUsername = textBoxUsername.Text;
            steamPassword = textBoxPassword.Text;
            
            if (checkBoxSteamGuard.Checked)
            {
                steamGuardCode = textBoxSteamGuard.Text;
            }
            
            isRunning = true;
            steamClient.Connect();

            while (isRunning)
            {
                // in order for the callbacks to get routed, they need to be handled by the manager
                callbackManager.RunWaitCallbacks(TimeSpan.FromSeconds(1));
            }

            buttonLogin.Enabled = true;
        }

        private void btnView_Click(object sender, EventArgs e)
        {
            if (textBoxPassword.PasswordChar == '*')
            {
                textBoxPassword.PasswordChar = '\0';
            }
            else
            {
                textBoxPassword.PasswordChar = '*';
            }
        }
    }
}
