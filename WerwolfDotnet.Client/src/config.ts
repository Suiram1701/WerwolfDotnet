import {Api, type ClientConfigDto} from "./Api";

let cachedConfig: ClientConfigDto | undefined;

export const config =
{
    // Has to be changed when publish (for example with Docker) 
    apiEndpoint: "http://localhost:7216", 
    sessionsPollInterval: 5,
    
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