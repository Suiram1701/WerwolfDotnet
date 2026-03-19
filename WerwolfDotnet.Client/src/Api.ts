/* eslint-disable */
/* tslint:disable */
/*
 * ---------------------------------------------------------------
 * ## THIS FILE WAS GENERATED VIA SWAGGER-TYPESCRIPT-API        ##
 * ##                                                           ##
 * ## AUTHOR: acacode                                           ##
 * ## SOURCE: https://github.com/acacode/swagger-typescript-api ##
 * ---------------------------------------------------------------
 */

/** @format int32 */
export enum ActionType {
  MayorVoting = 0,
  NextMayorDecision = 1,
  WerwolfAccuses = 2,
  WerwolfKilling = 3,
  WerwolfSelection = 4,
  SeerSelection = 5,
  WitchHealSelection = 6,
  WitchKillSelection = 7,
  HealerSelection = 8,
  HunterSelection = 9,
  AmorSelection = 10,
  VillageMattressSelection = 11,
}

/** @format int32 */
export enum CauseOfDeath {
  WerwolfKilling = 0,
  WerwolfKill = 1,
  WitchPoisoning = 2,
  WitchExplosion = 3,
  ShootByHunter = 4,
  DeathByHearthBreak = 5,
  None = -1,
}

export interface ClientConfigDto {
  sessionsVisible?: boolean;
  /** @format int32 */
  minimumPlayers?: number;
  /** @format int32 */
  playerNameMinLength?: number;
  gameMasterSkipAllowed?: boolean;
}

/** @format int32 */
export enum Fraction {
  Village = 0,
  Werwolf = 1,
  Lovers = 2,
}

/** A Dto for returning a game. */
export interface GameDto {
  /**
   * The identifier of the game
   * @format int32
   */
  id?: number;
  /** Indicates whether the game is password protected. */
  protected?: boolean;
  /** Indicates whether new players can join the game. */
  canJoin?: boolean;
  /** The name of the game master (the one who is hosting the game) */
  gameMaster?: string | null;
  /**
   * The current amount of players in the game.
   * @format int32
   */
  playerCount?: number;
  /**
   * The maximum amount of player in a game.
   * @format int32
   */
  maxPlayerCount?: number;
}

/** @format int32 */
export enum GameState {
  Locked = 0,
  Day = 1,
  Night = 2,
  GameWon = 3,
  NotInitialized = -2,
  Preparation = -1,
}

/** A Dto for creating or joining a gme. */
export interface JoinGameDto {
  /** The name of the name to add. */
  playerName?: string | null;
  /** An optional password to protect the session with. */
  gamePassword?: string | null;
}

export interface JoinedGameDto {
  /** A Dto for returning a game. */
  game: GameDto;
  self: PlayerDto;
  /** The base64 encoded JSON object build out of the game id, player id and WerwolfDotnet.Server.Models.JoinedGameDto.PlayerToken */
  bearerToken?: string | null;
  /** The URL of SignalR WebSocket to connect to. */
  signalrUrl?: string | null;
}

export interface PlayerDto {
  /**
   * The id (only unique in the game session) of the player.
   * @format int32
   */
  id?: number;
  /** The name of the player. */
  name?: string | null;
  role?: Role;
  /** Indicates whether this player is currently alive. */
  alive?: boolean;
}

/** @format int32 */
export enum PlayerRelation {
  None = 0,
  Ally = 1,
  Lover = 2,
}

/** @format int32 */
export enum Role {
  None = 0,
  Villager = 1,
  Seer = 2,
  SeerApprentice = 3,
  Witch = 4,
  Healer = 5,
  Hunter = 6,
  Amor = 7,
  VillageMattress = 8,
  TwoSisters = 9,
  ThreeBrothers = 10,
  BearGuide = 11,
  Werwolf = -1,
}

export interface SelectionOptionsDto {
  type?: ActionType;
  /**
   * The minimum amount of players to select per player.
   * @format int32
   */
  minimum?: number;
  /**
   * The maximum amount of players to select per player.
   * @format int32
   */
  maximum?: number;
  votablePlayers?: number[] | null;
}

export type QueryParamsType = Record<string | number, any>;
export type ResponseFormat = keyof Omit<Body, "body" | "bodyUsed">;

export interface FullRequestParams extends Omit<RequestInit, "body"> {
  /** set parameter to `true` for call `securityWorker` for this request */
  secure?: boolean;
  /** request path */
  path: string;
  /** content type of request body */
  type?: ContentType;
  /** query params */
  query?: QueryParamsType;
  /** format of response (i.e. response.json() -> format: "json") */
  format?: ResponseFormat;
  /** request body */
  body?: unknown;
  /** base url */
  baseUrl?: string;
  /** request cancellation token */
  cancelToken?: CancelToken;
}

export type RequestParams = Omit<FullRequestParams, "body" | "method" | "query" | "path">;

export interface ApiConfig<SecurityDataType = unknown> {
  baseUrl?: string;
  baseApiParams?: Omit<RequestParams, "baseUrl" | "cancelToken" | "signal">;
  securityWorker?: (securityData: SecurityDataType | null) => Promise<RequestParams | void> | RequestParams | void;
  customFetch?: typeof fetch;
}

export interface HttpResponse<D extends unknown, E extends unknown = unknown> extends Response {
  data: D;
  error: E;
}

type CancelToken = Symbol | string | number;

export enum ContentType {
  Json = "application/json",
  FormData = "multipart/form-data",
  UrlEncoded = "application/x-www-form-urlencoded",
  Text = "text/plain",
}

export class HttpClient<SecurityDataType = unknown> {
  public baseUrl: string = "";
  private securityData: SecurityDataType | null = null;
  private securityWorker?: ApiConfig<SecurityDataType>["securityWorker"];
  private abortControllers = new Map<CancelToken, AbortController>();
  private customFetch = (...fetchParams: Parameters<typeof fetch>) => fetch(...fetchParams);

  private baseApiParams: RequestParams = {
    credentials: "same-origin",
    headers: {},
    redirect: "follow",
    referrerPolicy: "no-referrer",
  };

  constructor(apiConfig: ApiConfig<SecurityDataType> = {}) {
    Object.assign(this, apiConfig);
  }

  public setSecurityData = (data: SecurityDataType | null) => {
    this.securityData = data;
  };

  protected encodeQueryParam(key: string, value: any) {
    const encodedKey = encodeURIComponent(key);
    return `${encodedKey}=${encodeURIComponent(typeof value === "number" ? value : `${value}`)}`;
  }

  protected addQueryParam(query: QueryParamsType, key: string) {
    return this.encodeQueryParam(key, query[key]);
  }

  protected addArrayQueryParam(query: QueryParamsType, key: string) {
    const value = query[key];
    return value.map((v: any) => this.encodeQueryParam(key, v)).join("&");
  }

  protected toQueryString(rawQuery?: QueryParamsType): string {
    const query = rawQuery || {};
    const keys = Object.keys(query).filter((key) => "undefined" !== typeof query[key]);
    return keys
      .map((key) => (Array.isArray(query[key]) ? this.addArrayQueryParam(query, key) : this.addQueryParam(query, key)))
      .join("&");
  }

  protected addQueryParams(rawQuery?: QueryParamsType): string {
    const queryString = this.toQueryString(rawQuery);
    return queryString ? `?${queryString}` : "";
  }

  private contentFormatters: Record<ContentType, (input: any) => any> = {
    [ContentType.Json]: (input: any) =>
      input !== null && (typeof input === "object" || typeof input === "string") ? JSON.stringify(input) : input,
    [ContentType.Text]: (input: any) => (input !== null && typeof input !== "string" ? JSON.stringify(input) : input),
    [ContentType.FormData]: (input: any) =>
      Object.keys(input || {}).reduce((formData, key) => {
        const property = input[key];
        formData.append(
          key,
          property instanceof Blob
            ? property
            : typeof property === "object" && property !== null
            ? JSON.stringify(property)
            : `${property}`,
        );
        return formData;
      }, new FormData()),
    [ContentType.UrlEncoded]: (input: any) => this.toQueryString(input),
  };

  protected mergeRequestParams(params1: RequestParams, params2?: RequestParams): RequestParams {
    return {
      ...this.baseApiParams,
      ...params1,
      ...(params2 || {}),
      headers: {
        ...(this.baseApiParams.headers || {}),
        ...(params1.headers || {}),
        ...((params2 && params2.headers) || {}),
      },
    };
  }

  protected createAbortSignal = (cancelToken: CancelToken): AbortSignal | undefined => {
    if (this.abortControllers.has(cancelToken)) {
      const abortController = this.abortControllers.get(cancelToken);
      if (abortController) {
        return abortController.signal;
      }
      return void 0;
    }

    const abortController = new AbortController();
    this.abortControllers.set(cancelToken, abortController);
    return abortController.signal;
  };

  public abortRequest = (cancelToken: CancelToken) => {
    const abortController = this.abortControllers.get(cancelToken);

    if (abortController) {
      abortController.abort();
      this.abortControllers.delete(cancelToken);
    }
  };

  public request = async <T = any, E = any>({
    body,
    secure,
    path,
    type,
    query,
    format,
    baseUrl,
    cancelToken,
    ...params
  }: FullRequestParams): Promise<HttpResponse<T, E>> => {
    const secureParams =
      ((typeof secure === "boolean" ? secure : this.baseApiParams.secure) &&
        this.securityWorker &&
        (await this.securityWorker(this.securityData))) ||
      {};
    const requestParams = this.mergeRequestParams(params, secureParams);
    const queryString = query && this.toQueryString(query);
    const payloadFormatter = this.contentFormatters[type || ContentType.Json];
    const responseFormat = format || requestParams.format;

    return this.customFetch(`${baseUrl || this.baseUrl || ""}${path}${queryString ? `?${queryString}` : ""}`, {
      ...requestParams,
      headers: {
        ...(requestParams.headers || {}),
        ...(type && type !== ContentType.FormData ? { "Content-Type": type } : {}),
      },
      signal: cancelToken ? this.createAbortSignal(cancelToken) : requestParams.signal,
      body: typeof body === "undefined" || body === null ? null : payloadFormatter(body),
    }).then(async (response) => {
      const r = response as HttpResponse<T, E>;
      r.data = null as unknown as T;
      r.error = null as unknown as E;

      const data = !responseFormat
        ? r
        : await response[responseFormat]()
            .then((data) => {
              if (r.ok) {
                r.data = data;
              } else {
                r.error = data;
              }
              return r;
            })
            .catch((e) => {
              r.error = e;
              return r;
            });

      if (cancelToken) {
        this.abortControllers.delete(cancelToken);
      }

      if (!response.ok) throw data;
      return data;
    });
  };
}

/**
 * @title WerwolfDotnet.Server
 * @version 1.0
 */
export class Api<SecurityDataType extends unknown> extends HttpClient<SecurityDataType> {
  api = {
    /**
     * No description
     *
     * @tags GameSession
     * @name GameSessionsCreate
     * @summary Creates a new game and the game master who created it.
     * @request POST:/api/game_sessions
     */
    gameSessionsCreate: (data: JoinGameDto, params: RequestParams = {}) =>
      this.request<JoinedGameDto, void>({
        path: `/api/game_sessions`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags GameSession
     * @name GameSessionsList
     * @summary Retrieves all existing game sessions. This endpoint can be disabled by the server.
     * @request GET:/api/game_sessions
     */
    gameSessionsList: (params: RequestParams = {}) =>
      this.request<GameDto[], void>({
        path: `/api/game_sessions`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags GameSession
     * @name GameSessionsDetail
     * @summary Retrieves a single game session by its id.
     * @request GET:/api/game_sessions/{sessionId}
     */
    gameSessionsDetail: (sessionId: number, params: RequestParams = {}) =>
      this.request<GameDto, void>({
        path: `/api/game_sessions/${sessionId}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags GameSession
     * @name GameSessionsPlayersCreate
     * @summary Creates a new player in an existing game session.
     * @request POST:/api/game_sessions/{sessionId}/players
     */
    gameSessionsPlayersCreate: (sessionId: number, data: JoinGameDto, params: RequestParams = {}) =>
      this.request<JoinedGameDto, void>({
        path: `/api/game_sessions/${sessionId}/players`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags GameSession
     * @name GameSessionsPlayersDetail
     * @summary Retrieves all players from a game.
     * @request GET:/api/game_sessions/{sessionId}/players
     */
    gameSessionsPlayersDetail: (sessionId: number, params: RequestParams = {}) =>
      this.request<PlayerDto[], void>({
        path: `/api/game_sessions/${sessionId}/players`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags GameSession
     * @name GameSessionsPlayersDetail2
     * @summary Retrieves a single player from a game.
     * @request GET:/api/game_sessions/{sessionId}/players/{playerId}
     * @originalName gameSessionsPlayersDetail
     * @duplicate
     */
    gameSessionsPlayersDetail2: (sessionId: number, playerId: number, params: RequestParams = {}) =>
      this.request<PlayerDto, void>({
        path: `/api/game_sessions/${sessionId}/players/${playerId}`,
        method: "GET",
        format: "json",
        ...params,
      }),

    /**
     * No description
     *
     * @tags GameSession
     * @name GameSessionsPlayersDelete
     * @summary Removes a player from an existing game session. You can always remove yourself, but to remove other players you need to be the game master.
     * @request DELETE:/api/game_sessions/{sessionId}/players/{playerId}
     * @secure
     */
    gameSessionsPlayersDelete: (sessionId: number, playerId: number, params: RequestParams = {}) =>
      this.request<void, void>({
        path: `/api/game_sessions/${sessionId}/players/${playerId}`,
        method: "DELETE",
        secure: true,
        ...params,
      }),

    /**
     * No description
     *
     * @tags Root
     * @name ConfigList
     * @summary Retrieves configuration used by the client which also depends on the server.
     * @request GET:/api/config
     */
    configList: (params: RequestParams = {}) =>
      this.request<ClientConfigDto, any>({
        path: `/api/config`,
        method: "GET",
        format: "json",
        ...params,
      }),
  };
}
