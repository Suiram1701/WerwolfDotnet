import { type SelectionOptionsDto, GameState, Role, CauseOfDeath, type PlayerDto } from "./Api";
import { HubConnection } from "@microsoft/signalr"

export class GameHubServer {
    private connection: HubConnection;
    
    constructor(connection: HubConnection) {
        this.connection = connection;
    }
    
    public async setGameLocked(locked: boolean): Promise<void> {
        await this.connection.invoke("setGameLocked", locked);
    }
    
    public async shufflePlayers(): Promise<void> {
        await this.connection.invoke("shufflePlayers");
    }
    
    public async startGame(): Promise<void> {
        await this.connection.invoke("startGame");
    }
    
    public async playerAction(selectedPlayer: number[]): Promise<void> {
        await this.connection.invoke("playerAction", selectedPlayer);
    }
    
    public async stopGame(): Promise<void> {
        await this.connection.invoke("stopGame");
    }
    
    public async leaveGame(playerId: number | null = null): Promise<void> {
        await this.connection.invoke("leaveGame", playerId);
    } 
}

export abstract class GameHubClientBase {
    protected constructor(connection: HubConnection) {
        connection.on("onGameMetaUpdated", this.onGameMetaUpdated);
        connection.on("onPlayersUpdated", this.onPlayersUpdated);
        connection.on("onGameStateUpdated", this.onGameStateUpdated);
        connection.on("onPlayerRoleUpdated", this.onPlayerRoleUpdated);
        connection.on("onActionRequested", this.onActionRequested);
        connection.on("onVotesUpdated", this.onVotesUpdated);
        connection.on("onActionCompleted", this.onActionCompleted);
        connection.on("onGameEnded", this.onGameEnded);
        connection.on("onForceDisconnect", this.onForceDisconnect);
    }

    public abstract onGameMetaUpdated(gameMasterId: number, mayorId: number | null): Promise<void>;
    
    public abstract onPlayersUpdated(players: PlayerDto[]): Promise<void>;
    
    public abstract onGameStateUpdated(newState: GameState, diedPlayers: Record<number, DeathDetails>): Promise<void>;
    
    public abstract onPlayerRoleUpdated(roleName: Role): Promise<void>;
    
    public abstract onActionRequested(action: SelectionOptionsDto): Promise<void>;
    
    public abstract onVotesUpdated(votes: Record<number, number[]>): Promise<void>;
    
    public abstract onActionCompleted(parameters: string[] | null): Promise<void>;
    
    public abstract onGameEnded(villageWin: boolean, playerRoles: Record<number, Role>): Promise<void>;
    
    public abstract onForceDisconnect(kicked: boolean): Promise<void>;
}

export type DeathDetails = { cause: CauseOfDeath, role: Role };