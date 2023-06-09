using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MikroTik.Types;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

using CacheManagment;


namespace MikroTik.Client
{
    public class MikroTikRespondeRequest
    {
        public int statusCode {get; set; }
        public string data {get; set;}

        public MikroTikRespondeRequest(int _status, string _data)
        {
            statusCode = _status;
            data = _data;
        }        
    }


    public class MikroTikClientRestApi
    {

        protected string MainRouterUrl;
        protected HttpClient client;
        protected string LastError;
        protected string addressServer;

        protected bool callEvents = true;


        public delegate void onEventError(string message);
        public delegate void onEventRequest(string url, Object data);
        public delegate void onEventResponde(int statusCode, Object data);
        public delegate void onEventAck(Object data);
        public delegate void onEventSuccess(Object data);
        public delegate void onEventFailed(Object data);

        public event onEventError OnEventError;
        public event onEventRequest OnEventRequest;
        public event onEventResponde OnEventResponde;
        public event onEventSuccess OnEventSuccess;
        public event onEventFailed OnEventFailed;



        public MikroTikClientRestApi(string address, string user, string passw)
        {
            LastError = "";
            addressServer = address;

            OnEventFailed = null;
            OnEventRequest = null;
            OnEventResponde = null;
            OnEventSuccess = null;
            OnEventError = null;


            MainRouterUrl = String.Format("https://{0}/rest/", address);

            var byteArray = Encoding.ASCII.GetBytes(user + ":" + passw);

            var httpClientHandler = new HttpClientHandler();
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
            {
                return true;
            };

            client = new HttpClient(httpClientHandler);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        public String GetLastErrorMessage()
        {
            return LastError;
        }


        private async Task<MikroTikRespondeRequest> SendPost(string url, object data)
        {
            LastError = "";
            HttpResponseMessage response;
            StringContent content = null;
            try
            {
                Debug.WriteLine("POST:" + url);

                if (data != null)
                {
                    var payload = JsonSerializer.Serialize(data);
                    Debug.WriteLine("DATA:" + payload.ToString());
                    content = new StringContent(payload, Encoding.UTF8, "application/json");
                }

                if (OnEventRequest != null && callEvents)
                    OnEventRequest(url, data);

                response = await client.PostAsync(url, content);

                Debug.WriteLine(" -> RESPONDE CODE:" + (int)response.StatusCode);

                var json = "";
                if ((int)response.StatusCode == 200 || (int)response.StatusCode == 201)
                {
                    json = await response.Content.ReadAsStringAsync();
                    json = json.Replace("\0", "");
                    Debug.WriteLine(" -> RESPONDE DATA:" + json);
                }
                else
                {
                    var errorMessage = new mtErrorMessage((int)response.StatusCode, response.StatusCode.ToString(), "");
                    json = errorMessage.ToString();
                    LastError = "Error: Server responde code" + response.StatusCode.ToString();
                }



                MikroTikRespondeRequest responde = new MikroTikRespondeRequest((int)response.StatusCode, json);
                return responde;
            }
            catch (Exception e)
            {
                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return null;
        }


        private async Task<MikroTikRespondeRequest> SendPut(string url, object data)
        {
            LastError = "";
            HttpResponseMessage response;
            StringContent content = null;
            try
            {

                if (data != null)
                {
                    var payload = JsonSerializer.Serialize(data);
                    content = new StringContent(payload, Encoding.UTF8, "application/json");
                }

                if (OnEventRequest != null && callEvents)
                    OnEventRequest(url, data);

                response = await client.PutAsync(url, content);

                var json = "";
                if ((int)response.StatusCode == 200 || (int)response.StatusCode == 201)
                {
                    json = await response.Content.ReadAsStringAsync();
                    json = json.Replace("\0", "");
                    Debug.WriteLine(" -> RESPONDE DATA:" + json);

                    var cache = new DataCache();
                    cache.LoadConfig();
                    var cacheData = cache.Find(url);
                    if (cacheData != null)
                        cache.RemoveCacheFile(url);
                }
                else
                {
                    var errorMessage = new mtErrorMessage((int)response.StatusCode, response.StatusCode.ToString(), "");
                    json = errorMessage.ToString();
                    LastError = "Error: Server responde code" + response.StatusCode.ToString();
                }


                MikroTikRespondeRequest responde = new MikroTikRespondeRequest((int)response.StatusCode, json);
                return responde;
            }
            catch (Exception e)
            {
                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return null;
        }


        private async Task<MikroTikRespondeRequest> SendPatch(string url, object data)
        {
            LastError = "";
            HttpResponseMessage response;
            StringContent content = null;
            try
            {
                Debug.WriteLine("PATCH:" + url);

                if (data != null)
                {
                    var payload = JsonSerializer.Serialize(data);
                    Debug.WriteLine("DATA:" + payload.ToString());
                    content = new StringContent(payload, Encoding.UTF8, "application/json");
                }

                if (OnEventRequest != null && callEvents)
                    OnEventRequest(url, data);

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };

                response = await client.SendAsync(request);

                Debug.WriteLine(" -> RESPONDE CODE:" + (int)response.StatusCode);


                var json = "";
                if ((int)response.StatusCode == 200 || (int)response.StatusCode == 201)
                {
                    json = await response.Content.ReadAsStringAsync();
                    json = json.Replace("\0", "");
                    Debug.WriteLine(" -> RESPONDE DATA:" + json);
                    var cache = new DataCache();
                    cache.LoadConfig();
                    var cacheData = cache.Find(url);
                    if (cacheData != null)
                        cache.RemoveCacheFile(url);
                }
                else
                {
                    var errorMessage = new mtErrorMessage((int)response.StatusCode, response.StatusCode.ToString(), "");
                    json = errorMessage.ToString();
                    LastError = "Error: Server responde code" + response.StatusCode.ToString();
                }

                MikroTikRespondeRequest responde = new MikroTikRespondeRequest((int)response.StatusCode, json);
                return responde;
            }
            catch (Exception e)
            {
                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(e.Message);
            }

            return null;
        }

        private async Task<MikroTikRespondeRequest> SendGet(string url)
        {
            LastError = "";
            HttpResponseMessage response;
            MikroTikRespondeRequest res;

            var cache = new DataCache();
            cache.LoadConfig();
            var cacheData = cache.Find(url);
            if (cacheData != null)
            {
                if (!cache.IsRenewCache(cacheData))
                {
                    var value = cache.GetCacheValue(cacheData);
                    if (value != null)
                    {
                        res = new MikroTikRespondeRequest(200, value);
                        return res;
                    }

                }
            }

            try
            {
                Debug.WriteLine("GET " + url);
                if (OnEventRequest != null && callEvents)
                    OnEventRequest(url, null);

                response = await client.GetAsync(url);


                var json = "";
                if ((int)response.StatusCode == 200)
                {
                    json = await response.Content.ReadAsStringAsync();
                    json = json.Replace("\0", "");
                    Debug.WriteLine(" -> RESPONDE DATA:" + json);

                    if (cacheData == null)
                        cache.SaveCacheValue(new CacheProperty(url), json);
                    else
                        cache.UpdateCacheValue(cacheData, json);
                }
                    else
                {
                    var errorMessage = new mtErrorMessage((int)response.StatusCode, response.StatusCode.ToString(), "");
                    json = errorMessage.ToString();
                    LastError = "Error: Server responde code" + response.StatusCode.ToString();
                }

                res = new MikroTikRespondeRequest((int)response.StatusCode, json);
                return res;
            }
            catch (Exception e)
            {
                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return null;
        }


      private async Task<MikroTikRespondeRequest> SendDelete(string url)
        {
            LastError = "";
            HttpResponseMessage response;
            StringContent content = null;
            try
            {

                if (OnEventRequest != null && callEvents)
                    OnEventRequest(url, null);

                response = await client.DeleteAsync(url);
                Debug.WriteLine("Delete status code=" + (int)response.StatusCode);

                var json = "";
                if ((int)response.StatusCode == 204)
                {
                    json = await response.Content.ReadAsStringAsync();
                    json = json.Replace("\0", "");
                    Debug.WriteLine(" -> RESPONDE DATA:" + json);

                    var cache = new DataCache();
                    cache.LoadConfig();
                    var cacheData = cache.Find(url);
                    if (cacheData != null) 
                        cache.RemoveCacheFile(url);
                }
                else
                {
                    var errorMessage = new mtErrorMessage((int)response.StatusCode, response.StatusCode.ToString(), "");
                    json = errorMessage.ToString();
                    LastError = "Error: Server responde code" + response.StatusCode.ToString();
                }


                MikroTikRespondeRequest responde = new MikroTikRespondeRequest((int)response.StatusCode, json);
                return responde;
            }
            catch (Exception e)
            {
                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return null;
        }


        /***************************************************************************************************
               DELETE USER FROM MIKROTIK USER MANAGER
        ****************************************************************************************************/
        public async Task<(bool, object)> RemoveUserFromUserManagerAsync(string id)
        {
            string Url = String.Format("{0}user-manager/user/{1}", MainRouterUrl, id);
            Debug.WriteLine("DeleteUserAccount Connecting: " + Url);


            try
            {
                MikroTikRespondeRequest responde = await SendDelete(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 204)
                {

                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, null);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(null);
                    }

                    return (true, null);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {
                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }  

        public bool RemoveUserFromUserManager(string id)
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;

                var t = Task.Run(() =>
                {
                    return RemoveUserFromUserManagerAsync(id);
                });
                (res, data) = t.Result;

                callEvents = true;
                return res;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return false;
        }


        /***************************************************************************************************
               GET USER FROM MIKROTIK USER MANAGER
        ****************************************************************************************************/

        public async Task<(bool, object)> GetUserFromUserManagerAsync(string id)
        {
            string Url = String.Format("{0}user-manager/user/{1}", MainRouterUrl, id);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<mtUserManagerInfo>(responde.data.ToString());
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {
                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public mtUserManagerInfo GetUserFromUserManager(string id)
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;

                var t = Task.Run(() =>
                {
                    return GetUserFromUserManagerAsync(id);
                });
                (res, data) = t.Result;

                callEvents = true;
                if (res)
                    return (mtUserManagerInfo)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }



        /***************************************************************************************************
             GET ALL USERS FROM MIKROTIK USER MANAGER
          ****************************************************************************************************/

        public async Task<(bool, object)> GetUsersFromUserManagerAsync()
        {
            string Url = String.Format("{0}user-manager/user", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<List<mtUserManagerInfo>>(responde.data.ToString());
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public List<mtUserManagerInfo> GetUsersFromUserManager()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;

                var t = Task.Run(() =>
                {
                    return GetUsersFromUserManagerAsync();
                });
                (res, data) = t.Result;

                callEvents = true;
                if (res)
                    return (List<mtUserManagerInfo>)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }



        /***************************************************************************************************
                  GET ALL GROUPS FROM MIKROTIK USER MANAGER
               ****************************************************************************************************/

        public async Task<(bool, object)> GetGroupsFromUserManagerAsync()
        {
            string Url = String.Format("{0}user-manager/user/group", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<List<mtUserManagerGroup>>(responde.data.ToString());
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {
                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public List<mtUserManagerGroup> GetGroupsFromUserManager()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;

                var t = Task.Run(() =>
                {
                    return GetGroupsFromUserManagerAsync();
                });
                (res, data) = t.Result;

                callEvents = true;
                if (res)
                    return (List<mtUserManagerGroup>)data;
            }
            catch (Exception e)
            {
                callEvents = true;
                LastError = "Exception:" + e.Message;
            }

            return (null);
        }


        /***************************************************************************************************
                  GET  GROUP FROM MIKROTIK USER MANAGER
               ****************************************************************************************************/

        public async Task<(bool, object)> GetGroupFromUserManagerAsync(string id)
        {
            string Url = String.Format("{0}user-manager/user/group/{1}", MainRouterUrl,id);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<mtUserManagerGroup>(responde.data.ToString());
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public mtUserManagerGroup GetGroupFromUserManager(string id)
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;

                var t = Task.Run(() =>
                {
                    return GetGroupFromUserManagerAsync(id);
                });
                (res, data) = t.Result;

                callEvents = true;
                if (res)
                    return (mtUserManagerGroup)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }


        /***************************************************************************************************
             GET Neighbors
          ****************************************************************************************************/

        public async Task<(bool, object)> GetNeighborsAsync()
        {
            string Url = String.Format("{0}ip/neighbor", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {

                    var data = JsonSerializer.Deserialize<List<mtNeighborInfo>>(responde.data);
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public List<mtNeighborInfo> GetNeighbors()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return GetNeighborsAsync();
                });
                (res, data) = t.Result;

                callEvents = true;

                if (res)
                    return (List<mtNeighborInfo>)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);

        }



        /***************************************************************************************************
                 GET DHCP Server lease
        ****************************************************************************************************/

        public async Task<(bool, object)> GetDhcpServerLeaseAsync()
        {
            string Url = String.Format("{0}ip/dhcp-server/lease", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {

                    var data = JsonSerializer.Deserialize<List<mtDHCPServerLease>>(responde.data);
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public List<mtDHCPServerLease> GetDhcpServerLease()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return GetDhcpServerLeaseAsync();
                });
                (res, data) = t.Result;

                callEvents = true;

                if (res)
                    return (List<mtDHCPServerLease>)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);

        }




        /***************************************************************************************************
             GET Interfaces
          ****************************************************************************************************/

        public async Task<(bool, object)> GetInterfacesAsync()
        {
            string Url = String.Format("{0}interface", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<List<mtInterfaceInfo>>(responde.data.ToString());
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public List<mtInterfaceInfo> GetInterfaces()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return GetInterfacesAsync();
                });
                (res, data) = t.Result;

                callEvents = true;

                if (res)
                    return (List<mtInterfaceInfo>)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception: " + e.Message;
            }

            return (null);
        }




        /***************************************************************************************************
             GET Ethernets
          ****************************************************************************************************/

        public async Task<(bool, object)> GetEthernetsAsync()
        {
            string Url = String.Format("{0}interface/ethernet", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<List<mtEthernetInfo>>(responde.data.ToString());
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public List<mtEthernetInfo> GetEthernets()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return GetEthernetsAsync();
                });
                (res, data) = t.Result;

                callEvents = true;

                if (res)
                    return (List<mtEthernetInfo>)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception: " + e.Message;
            }

            return (null);
        }




        /***************************************************************************************************
              COMMAD POST: UPGRADE CAP
          ****************************************************************************************************/

        public async Task<(bool, object)> ExecuteUpgradeCAPAsync(string id)
        {
            string Url = String.Format("{0}caps-man/remote-cap/upgrade", MainRouterUrl);

            try
            {
                mtRequestDataId data = new mtRequestDataId(id);

                MikroTikRespondeRequest responde = await SendPost(Url, data);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, null);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(null);
                    }

                    return (true, null);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(e.Message);
            }

            return (false, null);
        }

        public bool ExecuteUpgradeCAP(string id)
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;

                var t = Task.Run(() =>
                {
                    return ExecuteUpgradeCAPAsync(id);
                });
                (res, data) = t.Result;

                callEvents = true;

                return res;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return false;
        }


        /***************************************************************************************************
             GET ALL PACKAGE INSTALLED FROM MIKROTIK 
          ****************************************************************************************************/

        public async Task<(bool, object)> GetPackageAsync()
        {
            string Url = String.Format("{0}system/package", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<List<mtPackageInfo>>(responde.data.ToString());
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();


                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public List<mtPackageInfo> GetPackage()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return GetPackageAsync();
                });
                (res, data) = t.Result;

                callEvents = true;

                if (res)
                    return (List<mtPackageInfo>)data;

            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }

        /***************************************************************************************************
             GET ALL CONFIGURATIONS - CAPSMAN
         ****************************************************************************************************/

        public async Task<(bool, object)> GetConfigurationFromCAPsMANAsync()
        {
            string Url = String.Format("{0}caps-man/configuration", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<List<mtCAPsMANConfigurationInfo>>(responde.data.ToString());

                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, null);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(null);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.Message;

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }



        public List<mtCAPsMANConfigurationInfo> GetConfigurationFromCAPsMAN()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return GetConfigurationFromCAPsMANAsync();
                });
                (res, data) = t.Result;

                callEvents = true;
                if (res)
                    return (List<mtCAPsMANConfigurationInfo>)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }



        /***************************************************************************************************
                 GET ALL SCRIPTS
        ****************************************************************************************************/

        public async Task<(bool, object)> GetScriptsAsync()
        {
            string Url = String.Format("{0}system/script", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<List<mtScriptInfo>>(responde.data.ToString());
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }
                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);


                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public List<mtScriptInfo> GetScripts()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                                    {
                                        return GetScriptsAsync();
                                    });
                (res, data) = t.Result;

                callEvents = true;
                if (res)
                    return (List<mtScriptInfo>)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }


        /***************************************************************************************************
                 EXECUTE SCRIPT
        ****************************************************************************************************/

        public async Task<(bool, object)> ExecuteScriptAsync(string id)
        {
            string Url = String.Format("{0}system/script/run", MainRouterUrl);

            try
            {
                mtRequestDataId request = new mtRequestDataId(id);

                MikroTikRespondeRequest responde = await SendPost(Url, request);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {

                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, null);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(null);
                    }

                    return (true, null);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();


                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public bool ExecuteScript(string id)
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return ExecuteScriptAsync(id);
                });
                (res, data) = t.Result;

                callEvents = true;

                return res;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = e.Message;
            }

            return (false);
        }

        /***************************************************************************************************
               DELETE SCRIPT
        ****************************************************************************************************/
        public async Task<(bool, object)> DeleteScriptAsync(string id)
        {
            string Url = String.Format("{0}system/script/{1}", MainRouterUrl, id);

            try
            {
                MikroTikRespondeRequest responde = await SendDelete(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 204)
                {

                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, null);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(null);
                    }

                    return (true, null);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {
                LastError = "Exception: " + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public bool DeleteScript(string id)
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;

                var t = Task.Run(() =>
                {
                    return DeleteScriptAsync(id);
                });
                (res, data) = t.Result;

                callEvents = true;
                return res;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return false;
        }


        /***************************************************************************************************
              GET ALL INTERFACES  FROM CAPSMAN
           ****************************************************************************************************/

        public async Task<(bool, object)> GetInterfacesFromCAPsMANAsync()
        {
            string Url = String.Format("{0}caps-man/interface", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<List<mtCAPsMANInterfaceInfo>>(responde.data.ToString());
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public List<mtCAPsMANInterfaceInfo> GetInterfacesFromCAPsMAN()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return GetInterfacesFromCAPsMANAsync();
                });
                (res, data) = t.Result;

                callEvents = true;
                if (res)
                    return (List<mtCAPsMANInterfaceInfo>)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }


        /***************************************************************************************************
              GET ALL REMOTE CAP FROM CAPSMAN
           ****************************************************************************************************/

        public async Task<(bool, object)> GetRemoteCAPFromCAPsMANAsync()
        {
            string Url = String.Format("{0}caps-man/remote-cap", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<List<mtCAPsMANRemoteCAPInfo>>(responde.data.ToString());
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public List<mtCAPsMANRemoteCAPInfo> GetRemoteCAPFromCAPsMAN()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return GetRemoteCAPFromCAPsMANAsync();
                });
                (res, data) = t.Result;
                callEvents = true;

                if (res)
                    return (List<mtCAPsMANRemoteCAPInfo>)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }



        /***************************************************************************************************
              GET REGISTRION TABLE FROM CAPSMAN
           ****************************************************************************************************/

        public async Task<(bool, object)> GetRegistrionTableFromCAPsMANAsync()
        {
            string Url = String.Format("{0}caps-man/registration-table", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<List<mtCAPsMANRegistrationTableInfo>>(responde.data.ToString());
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }

        public List<mtCAPsMANRegistrationTableInfo> GetRegistrionTableFromCAPsMAN()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return GetRegistrionTableFromCAPsMANAsync();
                });
                (res, data) = t.Result;

                callEvents = true;

                if (res)
                    return (List<mtCAPsMANRegistrationTableInfo>)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }


        /***************************************************************************************************
              CREATE NEW USER USERMANAGER
         ****************************************************************************************************/

        public async Task<(bool, object)> CreateUserToUserManagerAsync(mtRequestNewUserData user)
        {
            string Url = String.Format("{0}user-manager/user", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendPut(Url, user);
                if (responde == null) return (false, null);

                if (responde.statusCode == 201)
                {
                    var data = JsonSerializer.Deserialize<mtUserManagerInfo>(responde.data.ToString());
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }



        public mtUserManagerInfo CreateUserToUserManager(mtRequestNewUserData user)
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return CreateUserToUserManagerAsync(user);
                });
                (res, data) = t.Result;

                callEvents = true;
                if (res)
                    return (mtUserManagerInfo)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }



        /***************************************************************************************************
                 UPDATE  USER USERMANAGER
        ****************************************************************************************************/

        public async Task<(bool, object)> UpdateeUserToUserManagerAsync(string id, mtRequestNewUserData user)
        {
            string Url = String.Format("{0}user-manager/user/{1}", MainRouterUrl, id);
            try
            {
                MikroTikRespondeRequest responde = await SendPut(Url, user);
                if (responde == null) return (false, null);

                if (responde.statusCode == 201)
                {
                    var data = JsonSerializer.Deserialize<mtUserManagerInfo>(responde.data.ToString());
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }



        public mtUserManagerInfo UpdateUserToUserManager(string id, mtRequestNewUserData user)
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return UpdateeUserToUserManagerAsync(id, user);
                });
                (res, data) = t.Result;

                callEvents = true;
                if (res)
                    return (mtUserManagerInfo)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }


        /***************************************************************************************************
             UPGRADE PACKAGES - SYSTEM
         ****************************************************************************************************/

        public async Task<(bool, object)> UpgradePackagesAsync()
        {
            string Url = String.Format("{0}system/package/update", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendPost(Url, null);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, null);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(null);
                    }

                    return (true, null);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }



        public bool UpgradePackages()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return UpgradePackagesAsync();
                });
                (res, data) = t.Result;
                callEvents = true;

                return res;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (false);
        }

        /***************************************************************************************************
             UPGRADE FIRMWARE - SYSTEM
         ****************************************************************************************************/

        public async Task<(bool, object)> UpgradeFirmwareAsync()
        {
            string Url = String.Format("{0}system/routerboard/upgrade", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendPost(Url, null);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, null);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(null);
                    }

                    return (true, null);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }



        public bool UpgradeFirmware()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return UpgradeFirmwareAsync();
                });
                (res, data) = t.Result;

                callEvents = true;
                return res;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (false);
        }


        /***************************************************************************************************
             REBOOTE - SYSTEM
         ****************************************************************************************************/

        public async Task<(bool, object)> ExecuteRebootAsync()
        {
            string Url = String.Format("{0}system/reboot", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendPost(Url, null);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, null);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(null);
                    }

                    return (true, null);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }



        public bool ExecuteReboot()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;

                var t = Task.Run(() =>
                {
                    return ExecuteRebootAsync();
                });
                (res, data) = t.Result;
                callEvents = true;

                return res;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (false);
        }



        /***************************************************************************************************
             GET ALL SCHEDULER -
         ****************************************************************************************************/

        public async Task<(bool, object)> GetSchedulersAsync()
        {
            string Url = String.Format("{0}system/scheduler", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<List<mtSchedulerInfo>>(responde.data.ToString());

                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.Message;

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }



        public List<mtSchedulerInfo> GetSchedulers()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return GetSchedulersAsync();
                });

                (res, data) = t.Result;

                callEvents = true;
                if (res)
                    return (List<mtSchedulerInfo>)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }


        /***************************************************************************************************
              NEW SCHEDULER
         ****************************************************************************************************/

        public async Task<(bool, object)> CreateSchedulerAsync(mtNewScheduler scheduler)
        {
            string Url = String.Format("{0}system/scheduler", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendPut(Url, scheduler);
                if (responde == null) return (false, null);

                Debug.WriteLine("[CreateSchedulerAsync] Responde StatusCode=" + responde.statusCode.ToString());

                if (responde.statusCode == 201)
                {
                    var data = JsonSerializer.Deserialize<mtSchedulerInfo>(responde.data.ToString());
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }


                Debug.WriteLine("ERROR ");
                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();


                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }


            Debug.WriteLine("EXIT NULL ");
            return (false, null);
        }



        public bool CreateScheduler(mtNewScheduler scheduler)
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return CreateSchedulerAsync(scheduler);
                });
                (res, data) = t.Result;

                if (data != null)
                    res = true;
                Debug.WriteLine("RESULT CreateScheduler= ", res);

                callEvents = true;
                return res;
            }
            catch (Exception e)
            {
                callEvents = true;
                Debug.WriteLine("Exceprion: " + e.Message);
                LastError = "Excpetion:" + e.Message;
            }

            return (false);
        }

        public mtSchedulerInfo CreateScheduler2(mtNewScheduler scheduler)
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return CreateSchedulerAsync(scheduler);
                });
                (res, data) = t.Result;
                Debug.WriteLine("result =  ", res);

                callEvents = true;
                if (res)
                    return (mtSchedulerInfo)data;
            }
            catch (Exception e)
            {
                callEvents = true;
                Debug.WriteLine("Exceprion: " + e.Message);
                LastError = "Exception:" + e.Message;
            }

            return (null);
        }



        /***************************************************************************************************
            UPDATE SCHEDULER
        ****************************************************************************************************/

        public async Task<(bool, object)> UpdateSchedulerAsync(string id, mtNewScheduler scheduler)
        {
            string Url = String.Format("{0}system/scheduler/{1}", MainRouterUrl, id);

            try
            {
                MikroTikRespondeRequest responde = await SendPatch(Url, scheduler);
                if (responde == null) return (false, null);

               var json =  JsonSerializer.Serialize(scheduler);
    
                Debug.WriteLine("[UpdateSchedulerAsync] Responde StatusCode=" + responde.statusCode.ToString());
                Debug.WriteLine(json);
                if (responde.statusCode == 200)
                {
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, null);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(null);
                    }

                    return (true, null);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();


                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }



        public bool UpdateScheduler(string id, mtNewScheduler scheduler)
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return UpdateSchedulerAsync(id, scheduler);
                });
                (res, data) = t.Result;

                callEvents = true;
                return res;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (false);
        }

      /*  public mtSchedulerInfo UpdateScheduler2(string id, mtNewScheduler scheduler)
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return UpdateSchedulerAsync(id, scheduler);
                });
                (res, data) = t.Result;

                callEvents = true;

                if (res)
                    return (mtSchedulerInfo)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }
        */


        /***************************************************************************************************
                DELETE SCHEDULER
            ****************************************************************************************************/

        public async Task<(bool, object)> DeleteSchedulerAsync(string id)
        {
            string Url = String.Format("{0}system/scheduler/{1}", MainRouterUrl, id);

            try
            {
                MikroTikRespondeRequest responde = await SendDelete(Url);
                if (responde == null) return (false, null);

                Debug.WriteLine("[UpdateSchedulerAsync] Responde StatusCode=" + responde.statusCode.ToString());
                if (responde.statusCode == 204)
                {
                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, null);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(null);
                    }

                    return (true, null);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.GetDetailMessage();


                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }



        public bool DeleteScheduler(string id)
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return DeleteSchedulerAsync(id);
                });
                (res, data) = t.Result;

                callEvents = true;
                return res;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (false);
        }

        /***************************************************************************************************
        GET ALL RADIUS SERVICES -
         ****************************************************************************************************/

        public async Task<(bool, object)> GetRadiusServicesAsync()
        {
            string Url = String.Format("{0}radius", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<List<mtRadiusService>>(responde.data.ToString());

                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.Message;

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }



        public List<mtRadiusService> GetRadiusServices()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return GetRadiusServicesAsync();
                });

                (res, data) = t.Result;

                callEvents = true;
                if (res)
                    return (List<mtRadiusService>)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }


        /***************************************************************************************************
        GET ALL RADIUS ROUTE -
         ****************************************************************************************************/

        public async Task<(bool, object)> GetRadiusRouterAsync()
        {
            string Url = String.Format("{0}user-manager/router", MainRouterUrl);

            try
            {
                MikroTikRespondeRequest responde = await SendGet(Url);
                if (responde == null) return (false, null);

                if (responde.statusCode == 200)
                {
                    var data = JsonSerializer.Deserialize<List<mtRadiusRouter>>(responde.data.ToString());

                    if (OnEventResponde != null && callEvents)
                    {
                        OnEventResponde(responde.statusCode, data);
                    }

                    if (OnEventSuccess != null && callEvents)
                    {
                        OnEventSuccess(data);
                    }

                    return (true, data);
                }

                var error = JsonSerializer.Deserialize<mtErrorMessage>(responde.data.ToString());
                LastError = error.Message;

                if (OnEventResponde != null && callEvents)
                    OnEventResponde(responde.statusCode, error);

                if (OnEventFailed != null && callEvents)
                    OnEventFailed(error);

                return (false, error);
            }
            catch (Exception e)
            {

                LastError = "Exception:" + e.Message;
                if (OnEventError != null && callEvents)
                    OnEventError(LastError);
            }

            return (false, null);
        }



        public List<mtRadiusRouter> GetRadiusRouter()
        {
            bool res = false;
            object data;
            try
            {
                callEvents = false;
                var t = Task.Run(() =>
                {
                    return GetRadiusRouterAsync();
                });

                (res, data) = t.Result;

                callEvents = true;
                if (res)
                    return (List<mtRadiusRouter>)data;
            }
            catch (Exception e)
            {
                callEvents = true;

                LastError = "Exception:" + e.Message;
            }

            return (null);
        }

    }
}