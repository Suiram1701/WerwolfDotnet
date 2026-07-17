import { Api, type ClientConfigDto } from "./Api";

let cachedConfig: ClientConfigDto | undefined;

export const config =
{
    apiEndpoint: import.meta.env.DEV ? "http://localhost:7216" : "",
    apiPollInterval: 5000,
    version: "0.4.0-dev",
    
    allowMultipleSessions: import.meta.env.DEV,
    allowSessionSharing: true,
    
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