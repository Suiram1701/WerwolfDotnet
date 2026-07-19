import { Api } from "../Api";
import { config, loadServerConfig } from "../config";

export const ssr = false;
export const prerender = false;

export async function load() {
    const apiClient = new Api({ baseUrl: config.apiEndpoint });
    await loadServerConfig(apiClient);
}