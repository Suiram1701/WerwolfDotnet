const storageKey = "gameSessions";

interface GameSession {
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

export function getPlayerToken(sessionId: number, playerId: number): string | undefined {
    const sessions: GameSession[] = JSON.parse(localStorage.getItem(storageKey) ?? "[]");
    const session: GameSession | undefined = sessions.find(game => game.sessionId === sessionId && game.playerId === playerId);
    return session?.playerToken;
}

export function removePlayerToken(sessionId: number, playerId: number): boolean {
    let sessions: GameSession[] = JSON.parse(localStorage.getItem(storageKey) ?? "[]");
    const session: GameSession | undefined = sessions.find(game => game.sessionId === sessionId && game.playerId === playerId);
    if (session === undefined)
        return false;
    
    sessions.splice(sessions.indexOf(session), 1);
    localStorage.setItem(storageKey, JSON.stringify(session));
    return true;
}