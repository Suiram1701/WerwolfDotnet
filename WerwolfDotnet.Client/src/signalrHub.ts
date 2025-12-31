import { type PlayerDto } from "./Api";
import { HubConnection } from "@microsoft/signalr"

export class GameHubServer {
    private connection: HubConnection;
    
    constructor(connection: HubConnection) {
        this.connection = connection;
    }
    
    public leaveGame(): void {
        this.connection.invoke("leaveGame", );
    }
}

export abstract class GameHubClientBase {
    private connection: HubConnection;
    
    protected constructor(connection: HubConnection) {
        this.connection = connection;
        connection.on("onPlayersUpdated", this.onPlayersUpdated)
    }

    public abstract onPlayersUpdated(players: PlayerDto[]): Promise<void>;
}