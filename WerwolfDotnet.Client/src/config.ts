import { Api, type ClientConfigDto } from "./Api";

let clientConfig: ClientConfigDto | undefined;

export const config =
{
    isDevelopment: import.meta.env.DEV,
    apiEndpoint: import.meta.env.DEV ? "http://localhost:7216" : "",
    apiPollInterval: 5000,
    version: "0.4.0-dev",
    
    allowMultipleSessions: import.meta.env.DEV,
    allowSessionSharing: true,
};

export function getServerConfig(): ClientConfigDto {
    if (!clientConfig) 
        throw new Error("Config not loaded! Call loadServerConfig() first!");
    return clientConfig;
}

export async function loadServerConfig(apiClient: Api<any>): Promise<void> {
    let response = await apiClient.api.configList();
    if (response !== undefined && response.ok)
        clientConfig = response.data;
    else 
        throw Error("Could not retrieve config");
}