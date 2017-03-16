import { Injectable, OnDestroy } from '@angular/core';
import { Router, UrlTree } from '@angular/router';
import { Observable, Subscription, Subject } from 'rxjs/Rx';
import { Http } from '@angular/http';

@Injectable()
export class UserService {
    private _bearerToken: string = "";
    private _clientId: string = "";
    private _serviceRoot: string = "";
    private _loginTimerSubscription: Subscription = null;
    private _loginScreenPath: string = "";
    private _isAuthenticated: boolean = false;
    private _authenticatedChangedSource:Subject<boolean> = new Subject<boolean>();

    public get BearerToken(): string { 
        if(this._bearerToken == "") this.EnsureLogin(); 
        return this._bearerToken; 
    } 
    public get ClientId():string { return this._clientId; }
    public set ClientId(val: string) {this._clientId = val; }

    public get EndPointBase():string { return this._serviceRoot; }
    public set EndPointBase(val: string) { this._serviceRoot = val; }

    public get IsAuthenticated(): boolean { return this._isAuthenticated; }
    public get AuthenticationChanged(): Observable<boolean> { return this._authenticatedChangedSource.asObservable(); }

    public get LoginScreenPath():string { return this._loginScreenPath; }
    public set LoginScreenPath(val: string) { this._loginScreenPath = val; }

    public get LoginEndPoint(): string {
        return '{0}/oauth/authorize?response_type=token&client_id={1}'.replace('{0}', this._serviceRoot).replace('{1}', this._clientId);
    }

    constructor(private _router: Router, clientId?: string, endpointBase?: string, loginScreenPath? :string) {
        this._clientId = clientId;
        this._serviceRoot = endpointBase;
        this._loginScreenPath = loginScreenPath;
     }

    public EnsureLogin(redirectAfterSuccessUrl?:string | UrlTree):string { 
        if(this._bearerToken == "") {
            // check route to make sure we are on the login page
            let path:string = "";
            if(this._router.routerState.root.firstChild) path = this._router.routerState.root.firstChild.snapshot.url.join('');
            if(this._loginScreenPath != '' && !this._loginScreenPath.toLowerCase().endsWith(path)) {
                this._router.navigateByUrl(this._loginScreenPath);
                return "";
            } 
            return this.Login(redirectAfterSuccessUrl);        
        }
        else {
           if(redirectAfterSuccessUrl) this._router.navigateByUrl(redirectAfterSuccessUrl); 
           return this._bearerToken;
        }    
    }



    public ngOnDestroy(){  }

    protected Login(redirectAfterSuccessUrl?:string | UrlTree): string{
        let timer: Observable<number> = null;
        let val = this._router.routerState.root.snapshot.fragment;
        let r:RegExp = new RegExp("([^?=&]+)(=([^&]*))?", "gi");
        let x:any = {};
        if(val != null && val != "") val.replace(r, (a:string ,b: string,c: string,d: string) => { x[b] = d; return x[b]; } )
        if(x['token_type'] == null || x['token_type'] != 'Bearer' || x['access_token'] == null){
            if(timer == null){
                timer = Observable.timer(2000);
                this._loginTimerSubscription = timer.subscribe(t => { this.RedirectToOAuthEndPoint(t); });
            }
        }
        else{
            this._bearerToken = x['access_token'];
            this._isAuthenticated = true;
            if(redirectAfterSuccessUrl) this._router.navigateByUrl(redirectAfterSuccessUrl);
        }
        this.ChangeAutenticationState(this._isAuthenticated);
        return this._bearerToken;
    }

    private ChangeAutenticationState(authenticated: boolean){
        this._authenticatedChangedSource.next(authenticated);
    }
    
    private RedirectToOAuthEndPoint(t: number){
        this._loginTimerSubscription.unsubscribe();
        window.location.href = this.LoginEndPoint;
    }

}