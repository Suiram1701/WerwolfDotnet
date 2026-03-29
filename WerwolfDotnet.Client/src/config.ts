import { Api, type ClientConfigDto } from "./Api";

let cachedConfig: ClientConfigDto | undefined;

export const config =
{
    apiEndpoint: ["127.0.0.1", "::1", "localhost"].includes(window.location.hostname) ? "http://localhost:7216" : "",
    sessionsPollInterval: 5,
    version: "0.2.0",
    
    getClientConfig: () => cachedConfig,
    retrieveConfigAsync: async (apiClient: Api<unknown>) => {
        if (cachedConfig !== undefined)
            return cachedConfig;

        let response = await apiClient.api.configList();
        if (response !== undefined && response.ok)
        {
            cachedConfig = response.data;
            return response.data;
        }
        else
        {
            throw Error("Could not retrieve config");
        }
    }
};