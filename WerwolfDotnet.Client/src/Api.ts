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
  WerwolfKilling = 1,
  WerwolfSelection = 2,
  SeerSelection = 3,
  WitchHealSelection = 4,
  WitchKillSelection = 5,
  HunterSelection = 6,
}

/** @format int32 */
export enum CauseOfDeath {
  WerwolfKilling = 0,
  WerwolfKill = 1,
  WitchPoisoning = 2,
  WitchExplosion = 3,
  ShootByHunter = 4,
  None = -1,
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
  /** A token used to authenticate as WerwolfDotnet.Server.Models.JoinedGameDto.Self against the API. */
  playerToken: string | null;
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
  /** Indicates whether this player is currently alive. */
  alive?: boolean;
}

/** @format int32 */
export enum Role {
  Villager = 0,
  Werwolf = 1,
  Seer = 2,
  Witch = 3,
  Hunter = 4,
  None = -1,
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
  excludedPlayers?: number[] | null;
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
     * @name GameSessionsJoinCreate
     * @summary Creates a new player in an existing game session.
     * @request POST:/api/game_sessions/{sessionId}/join
     */
    gameSessionsJoinCreate: (sessionId: number, data: JoinGameDto, params: RequestParams = {}) =>
      this.request<JoinedGameDto, void>({
        path: `/api/game_sessions/${sessionId}/join`,
        method: "POST",
        body: data,
        type: ContentType.Json,
        format: "json",
        ...params,
      }),
  };
}
