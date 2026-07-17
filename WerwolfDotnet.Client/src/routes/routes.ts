import { config } from "../config";

export default {
    menu: (kicked?: boolean): string => kicked === true ? "/?kicked=true" : "/",
    menuJoin: (sessionId: number): string => `/?gameId=${sessionId}`,
    game: (sessionId: number, playerId: number): string => config.allowMultipleSessions
        ? `/game?sessionId=${sessionId}&playerId=${playerId}`
        : `/game?sessionId=${sessionId}`,
    gameDirectJoin: (sessionId: number, playerId: number, token: string): string => `/game?sessionId=${sessionId}&playerId=${playerId}#auth=${token}`
}