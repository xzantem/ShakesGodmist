import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { tap, catchError } from 'rxjs/operators';
import { Player, Item, Quest, CreatePlayerDto, LoginRequest, RegisterRequest, AuthResponse, UserDto, Enemy } from '../models/game.models';

export interface BattleStep {
    attacker: 'player' | 'enemy' | 'init';
    damage: number;
    crit: boolean;
    targetHP: number;
    playerHP?: number;
    enemyHP?: number;
}

export interface CompleteQuestResult {
    player: Player;
    battleLog: string[];
    battleReplay: BattleStep[];
    playerWon: boolean;
    enemy: Enemy;
}

@Injectable({
    providedIn: 'root'
})
export class ApiService {
    private baseUrl = 'http://localhost:5166/api';
    private tokenKey = 'auth_token';
    private userKey = 'current_user';

    private currentUserSubject = new BehaviorSubject<UserDto | null>(null);
    public currentUser$ = this.currentUserSubject.asObservable();

    constructor(
        private http: HttpClient,
        @Inject(PLATFORM_ID) private platformId: Object
    ) {
        this.loadStoredAuth();
    }

    private loadStoredAuth(): void {
        if (!isPlatformBrowser(this.platformId)) return;

        const token = localStorage.getItem(this.tokenKey);
        const userStr = localStorage.getItem(this.userKey);

        // Simple token expiration check (JWTs are base64 encoded)
        function isTokenExpired(token: string): boolean {
            try {
                const payload = JSON.parse(atob(token.split('.')[1]));
                return payload.exp * 1000 < Date.now();
            } catch {
                return true;
            }
        }

        if (token && userStr && !isTokenExpired(token)) {
            try {
                const user = JSON.parse(userStr);
                this.currentUserSubject.next(user);
            } catch (e) {
                console.log('Failed to parse stored user, clearing auth');
                this.clearAuth();
            }
        } else {
            if (token || userStr) {
                console.log('Token expired or invalid, clearing auth');
                this.clearAuth();
            }
        }
    }

    private getAuthHeaders(): HttpHeaders {
        if (!isPlatformBrowser(this.platformId)) {
            return new HttpHeaders({ 'Content-Type': 'application/json' });
        }

        const token = localStorage.getItem(this.tokenKey);

        return new HttpHeaders({
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`
        });
    }

    private setAuth(authResponse: AuthResponse): void {
        if (!isPlatformBrowser(this.platformId)) return;

        localStorage.setItem(this.tokenKey, authResponse.token);
        localStorage.setItem(this.userKey, JSON.stringify(authResponse.user));
        this.currentUserSubject.next(authResponse.user);
    }

    private clearAuth(): void {
        if (!isPlatformBrowser(this.platformId)) return;

        localStorage.removeItem(this.tokenKey);
        localStorage.removeItem(this.userKey);
        this.currentUserSubject.next(null);
    }

    // Authentication endpoints
    login(loginRequest: LoginRequest): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${this.baseUrl}/auth/login`, loginRequest)
            .pipe(tap(response => this.setAuth(response)));
    }

    register(registerRequest: RegisterRequest): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${this.baseUrl}/auth/register`, registerRequest);
    }

    registerAndLogin(registerRequest: RegisterRequest): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${this.baseUrl}/auth/register`, registerRequest)
            .pipe(tap(response => this.setAuth(response)));
    }

    logout(): void {
        this.clearAuth();
    }

    getCurrentUser(): UserDto | null {
        return this.currentUserSubject.value;
    }

    isAuthenticated(): boolean {
        return this.getCurrentUser() !== null;
    }

    // Player endpoints (now require authentication)
    getPlayers(): Observable<Player[]> {
        if (!this.isAuthenticated()) {
            return throwError(() => new Error('Not authenticated'));
        }
        return this.http.get<Player[]>(`${this.baseUrl}/players`, { headers: this.getAuthHeaders() });
    }

    getPlayer(id: number): Observable<Player> {
        if (!this.isAuthenticated()) {
            return throwError(() => new Error('Not authenticated'));
        }
        return this.http.get<Player>(`${this.baseUrl}/players/${id}`, { headers: this.getAuthHeaders() });
    }

    createPlayer(playerDto: CreatePlayerDto): Observable<Player> {
        if (!this.isAuthenticated()) {
            return throwError(() => new Error('Not authenticated'));
        }
        return this.http.post<Player>(`${this.baseUrl}/players`, playerDto, { headers: this.getAuthHeaders() });
    }

    updatePlayer(player: Player): Observable<void> {
        if (!this.isAuthenticated()) {
            return throwError(() => new Error('Not authenticated'));
        }
        return this.http.put<void>(`${this.baseUrl}/players/${player.id}`, player, { headers: this.getAuthHeaders() });
    }

    deletePlayer(id: number): Observable<void> {
        if (!this.isAuthenticated()) {
            return throwError(() => new Error('Not authenticated'));
        }
        return this.http.delete<void>(`${this.baseUrl}/players/${id}`, { headers: this.getAuthHeaders() });
    }

    levelUpPlayer(id: number): Observable<Player> {
        if (!this.isAuthenticated()) {
            return throwError(() => new Error('Not authenticated'));
        }
        return this.http.post<Player>(`${this.baseUrl}/players/${id}/levelup`, {}, { headers: this.getAuthHeaders() });
    }

    // Quest endpoints
    getAvailableQuests(playerLevel: number = 1): Observable<Quest[]> {
        console.log('[DEBUG] getAvailableQuests - Fetching quests for level:', playerLevel);
        return this.http.get<Quest[]>(`${this.baseUrl}/quests?playerLevel=${playerLevel}`)
            .pipe(
                tap(quests => {
                    console.log('[DEBUG] getAvailableQuests - Retrieved quests:', quests.length);
                    quests.forEach(quest => {
                        console.log('[DEBUG] getAvailableQuests - Quest:', quest.name, 'Duration:', quest.duration, 'XP:', quest.experienceReward, 'Gold:', quest.goldReward);
                    });
                }),
                catchError(error => {
                    console.log('[DEBUG] getAvailableQuests - Failed to fetch quests:', error);
                    return throwError(() => error);
                })
            );
    }

    getPlayerQuests(playerId: number): Observable<Quest[]> {
        console.log('[DEBUG] getPlayerQuests - Fetching quests for player:', playerId);
        return this.http.get<Quest[]>(`${this.baseUrl}/quests/player/${playerId}`)
            .pipe(
                tap(quests => {
                    console.log('[DEBUG] getPlayerQuests - Retrieved quests:', quests.length);
                }),
                catchError(error => {
                    console.log('[DEBUG] getPlayerQuests - Failed to fetch player quests:', error);
                    return throwError(() => error);
                })
            );
    }

    startQuest(quest: Quest, playerId: number): Observable<Quest> {
        console.log('[DEBUG] startQuest - Starting quest:', quest.name, 'for player:', playerId);
        return this.http.post<Quest>(`${this.baseUrl}/quests/start/${playerId}`, quest)
            .pipe(
                tap(startedQuest => {
                    console.log('[DEBUG] startQuest - Quest started successfully:', startedQuest.name, 'Started at:', startedQuest.startedAt);
                }),
                catchError(error => {
                    console.log('[DEBUG] startQuest - Failed to start quest:', error);
                    return throwError(() => error);
                })
            );
    }

    completeQuest(questId: number): Observable<CompleteQuestResult> {
        console.log('[DEBUG] completeQuest - Completing quest with ID:', questId);
        return this.http.post<CompleteQuestResult>(`${this.baseUrl}/quests/${questId}/complete`, {})
            .pipe(
                tap(result => {
                    console.log('[DEBUG] completeQuest - Quest completed successfully. Player won:', result.playerWon);
                    console.log('[DEBUG] completeQuest - Battle log entries:', result.battleLog.length);
                }),
                catchError(error => {
                    console.log('[DEBUG] completeQuest - Failed to complete quest:', error);
                    return throwError(() => error);
                })
            );
    }

    grantQuestRewards(questId: number): Observable<{player: Player, grantedItem?: Item}> {
        console.log('[DEBUG] grantQuestRewards - Granting rewards for quest:', questId);
        return this.http.post<{player: Player, grantedItem?: Item}>(`${this.baseUrl}/quests/${questId}/grant-rewards`, {})
            .pipe(
                tap(result => {
                    console.log('[DEBUG] grantQuestRewards - Rewards granted successfully');
                    if (result.grantedItem) {
                        console.log('[DEBUG] grantQuestRewards - Item granted:', result.grantedItem.name);
                    }
                }),
                catchError(error => {
                    console.log('[DEBUG] grantQuestRewards - Failed to grant rewards:', error);
                    return throwError(() => error);
                })
            );
    }

    // Item endpoints
    getPlayerItems(playerId: number): Observable<Item[]> {
        return this.http.get<Item[]>(`${this.baseUrl}/items/player/${playerId}`);
    }

    getShopItems(playerLevel: number = 1): Observable<Item[]> {
        return this.http.get<Item[]>(`${this.baseUrl}/items/shop?playerLevel=${playerLevel}`);
    }

    buyItem(itemId: number, playerId: number): Observable<Player> {
        return this.http.post<Player>(`${this.baseUrl}/items/${itemId}/buy/${playerId}`, {});
    }

    equipItem(itemId: number): Observable<Player> {
        return this.http.post<Player>(`${this.baseUrl}/items/${itemId}/equip`, {});
    }

    sellItem(itemId: number): Observable<Player> {
        return this.http.post<Player>(`${this.baseUrl}/items/${itemId}/sell`, {});
    }

    buyStatUpgrade(playerId: number, stat: string): Observable<Player> {
        if (!this.isAuthenticated()) {
            return throwError(() => new Error('Not authenticated'));
        }
        return this.http.post<Player>(`${this.baseUrl}/players/${playerId}/buy-stat?stat=${stat}`, {}, { headers: this.getAuthHeaders() });
    }

    refreshShop(playerId: number): Observable<Item[]> {
        return this.http.post<Item[]>(`${this.baseUrl}/items/refresh-shop/${playerId}`, {});
    }
}
