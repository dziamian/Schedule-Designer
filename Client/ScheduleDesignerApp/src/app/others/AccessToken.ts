/**
 * Klasa odpowiedzialna za zapisywanie i odczytywanie tokenów dostępu do systemu USOS.
 */
export class AccessToken {
    /**
     * Klucz tokenu dostępu.
     */
    readonly key:string;

    /**
     * Sekret tokenu dostępu.
     */
    readonly secret:string;
    
    constructor(key:string, secret:string) {
        this.key = key;
        this.secret = secret;
    }

    /**
     * Metoda usuwająca token dostępu z lokalnego magazynu.
     */
    public static Remove():void {
        localStorage.removeItem('oauth_token');
        localStorage.removeItem('oauth_token_secret');
    }

    /**
     * Metoda usuwająca niezautoryzowany token z lokalnego magazynu.
     */
    public static RemoveRequest():void {
        localStorage.removeItem('request_oauth_token');
        localStorage.removeItem('request_oauth_token_secret');
    }

    /**
     * Metoda zapisująca token dostępu w lokalnym magazynie.
     */
    public Save():void {
        localStorage.setItem('oauth_token', this.key);
        localStorage.setItem('oauth_token_secret', this.secret);
    }

    /**
     * Metoda zapisująca niezautoryzowany token w lokalnym magazynie.
     */
    public SaveRequest():void {
        localStorage.setItem('request_oauth_token', this.key);
        localStorage.setItem('request_oauth_token_secret', this.secret);
    }

    /**
     * Metoda zapisująca token dostępu w lokalnym magazynie.
     * @param token Token dostępu
     */
    public static Save(token:AccessToken):void {
        localStorage.setItem('oauth_token', token.key);
        localStorage.setItem('oauth_token_secret', token.secret);
    }

    /**
     * Metoda zapisująca niezautoryzowany token w lokalnym magazynie.
     * @param token Niezautoryzowany token
     */
    public static SaveRequest(token:AccessToken):void {
        localStorage.setItem('request_oauth_token', token.key);
        localStorage.setItem('request_oauth_token_secret', token.secret);
    }

    /**
     * Metoda pobierająca token dostępu z lokalnego magazynu.
     * @returns Token dostępu lub wartość null jeśli nie został zapisany w magazynie
     */
    public static Retrieve():AccessToken|null {
        let key:string|null = localStorage.getItem('oauth_token');
        let secret:string|null = localStorage.getItem('oauth_token_secret');
        
        if (key == null || secret == null) {
            return null;
        }

        return new AccessToken(key, secret);
    }

    /**
     * Metoda pobierająca niezautoryzowany token z lokalnego magazynu.
     * @returns Niezautoryzowany token lub wartość null jeśli nie został zapisany w magazynie
     */
    public static RetrieveRequest():AccessToken|null {
        let key:string|null = localStorage.getItem('request_oauth_token');
        let secret:string|null = localStorage.getItem('request_oauth_token_secret');
        
        if (key == null || secret == null) {
            return null;
        }

        return new AccessToken(key, secret);
    }

    /**
     * Określa czy użytkownik posiada zapisany token dostępu.
     * @returns Prawdę jeśli użytkownik posiada zapisany token dostępu, w przeciwnym wypadku fałsz.
     */
    public static isAuthenticated():boolean {
        return this.Retrieve() != null;
    }

    /**
     * Metoda wyciągająca token z ciągu znaków.
     * @param text Ciąg znaków do przeanalizowania
     * @param preffixes Tablica prefixów określających klucz i sekret
     * @param delimiter Ogranicznik pomiędzy kluczem i sekretem
     * @returns Token wyciągnięty z ciągu znaków
     */
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

    /**
     * Zwraca obiekt zawierający klucz i sekret tokenu.
     * @returns Obiekt posiadający klucz i sekret tokenu
     */
    public ToJson():Object {
        return { key: this.key, secret: this.secret };
    }
}