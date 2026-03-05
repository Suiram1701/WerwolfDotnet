import { writable } from 'svelte/store';
import { GameState, type PlayerDto, PlayerRelation, type Role, type SelectionOptionsDto } from "../Api";

export interface GamePageState {
    gameId: number;
    selfId: number;
    selfRole: Role | undefined;
    
    players: PlayerDto[];
    playerRelations: Record<number, PlayerRelation[]>;
    gameState: GameState | undefined;
    gameMeta: { gameMaster: number, mayor: number | null } | undefined;
    
    currentAction: SelectionOptionsDto | null;
    selectedPlayers: number[];
    playerToVotes: Record<number, number[]>;
    emptyVotedPlayers: number[];
}

export const gamePageState = writable<GamePageState>({
    gameId: -1,
    selfId: -1,
    selfRole: undefined,
    
    players: [],
    playerRelations: [],
    gameState: undefined,
    gameMeta: undefined,
    
    currentAction: null,
    selectedPlayers: [],
    playerToVotes: [],
    emptyVotedPlayers: [],
});