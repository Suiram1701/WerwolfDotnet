import {type GameMetadataDto, type GameStateDto, type PlayerDto} from "./Api";
import { HubConnection } from "@microsoft/signalr"

export class GameHubServer {
    private connection: HubConnection;
    
    constructor(connection: HubConnection) {
        this.connection = connection;
    }
    
    public async leaveGame(playerId: number | null = null): Promise<void> {
        await this.connection.invoke("leaveGame", playerId);
    } 
}

export abstract class GameHubClientBase {
    private connection: HubConnection;
    
    protected constructor(connection: HubConnection) {
        this.connection = connection;
        connection.on("onGameMetaUpdated", this.onGameMetaUpdated);
        connection.on("onPlayersUpdated", this.onPlayersUpdated);
        connection.on("onGameStateUpdated", this.onGameStateUpdated);
        connection.on("onForceDisconnect", this.onForceDisconnect);
    }

    public abstract onGameMetaUpdated(meta: GameMetadataDto): Promise<void>;
    
    public abstract onPlayersUpdated(players: PlayerDto[]): Promise<void>;
    
    public abstract onGameStateUpdated(gameState: GameStateDto): Promise<void>;
    
    public abstract onForceDisconnect(kicked: boolean): Promise<void>;
}