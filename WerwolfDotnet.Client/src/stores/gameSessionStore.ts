const storageKey = "gameSessions";

export interface GameSession {
    sessionId: number,
    playerId: number,
    playerToken: string
}

export function storePlayerToken(sessionId: number, playerId: number, authToken: string ) {
    const newSession: GameSession = {
        sessionId: sessionId,
        playerId: playerId,
        playerToken: authToken
    };
    
    let existingSessions: GameSession[] = JSON.parse(localStorage.getItem(storageKey) ?? "[]");
    existingSessions.push(newSession);
    localStorage.setItem(storageKey, JSON.stringify(existingSessions));
}

export function getPlayerTokens(): GameSession[] {
    return JSON.parse(localStorage.getItem(storageKey) ?? "[]");
}

export function getPlayerToken(sessionId: number, playerId?: number): GameSession | undefined {
    const sessions: GameSession[] = JSON.parse(localStorage.getItem(storageKey) ?? "[]");
    return sessions.find(game => game.sessionId === sessionId && (playerId !== undefined ? game.playerId === playerId : true));
}

export function removePlayerToken(sessionId: number, playerId?: number): boolean {
    let sessions: GameSession[] = JSON.parse(localStorage.getItem(storageKey) ?? "[]");
    sessions = sessions.filter(game => !(game.sessionId === sessionId && (playerId !== undefined ? game.playerId === playerId : true)));
    
    localStorage.setItem(storageKey, JSON.stringify(sessions));
    return true;
}