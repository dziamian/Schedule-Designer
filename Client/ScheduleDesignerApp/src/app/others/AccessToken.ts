export class AccessToken {
    readonly key:string;
    readonly secret:string;
    
    constructor(key:string, secret:string) {
        this.key = key;
        this.secret = secret;
    }

    public static Remove():void {
        localStorage.removeItem('oauth_token');
        localStorage.removeItem('oauth_token_secret');
    }

    public static RemoveRequest():void {
        localStorage.removeItem('request_oauth_token');
        localStorage.removeItem('request_oauth_token_secret');
    }

    public Save():void {
        localStorage.setItem('oauth_token', this.key);
        localStorage.setItem('oauth_token_secret', this.secret);
    }

    public SaveRequest():void {
        localStorage.setItem('request_oauth_token', this.key);
        localStorage.setItem('request_oauth_token_secret', this.secret);
    }

    public static Save(token:AccessToken):void {
        localStorage.setItem('oauth_token', token.key);
        localStorage.setItem('oauth_token_secret', token.secret);
    }

    public static SaveRequest(token:AccessToken):void {
        localStorage.setItem('request_oauth_token', token.key);
        localStorage.setItem('request_oauth_token_secret', token.secret);
    }

    public static Retrieve():AccessToken|null {
        let key:string|null = localStorage.getItem('oauth_token');
        let secret:string|null = localStorage.getItem('oauth_token_secret');
        
        if (key == null || secret == null) {
            return null;
        }

        return new AccessToken(key, secret);
    }

    public static RetrieveRequest():AccessToken|null {
        let key:string|null = localStorage.getItem('request_oauth_token');
        let secret:string|null = localStorage.getItem('request_oauth_token_secret');
        
        if (key == null || secret == null) {
            return null;
        }

        return new AccessToken(key, secret);
    }

    public static isAuthenticated():boolean {
        return this.Retrieve() != null;
    }

    public static ParseToken(text:string, preffixes:string[], delimiter:string):AccessToken {
        if (preffixes.length != 2) {
            throw new Error("Invalid length of preffixes");
        }
        
        let values:string[] = [];
        for (let i:number = 0; i < 2; ++i) {
            let startIndex = text.indexOf(preffixes[i]) + preffixes[i].length;
            let endIndex = text.indexOf(delimiter, startIndex);
            if (endIndex < 0) {
                endIndex = text.length;
            }
            values.push(text.substring(startIndex, endIndex));
        }

        return new AccessToken(values[0], values[1]);
    }

    public ToJson():Object {
        return { key: this.key, secret: this.secret };
    }
}