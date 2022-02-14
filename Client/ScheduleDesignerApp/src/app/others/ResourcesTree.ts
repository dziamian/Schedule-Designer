import { Filter } from "./Filter";

/**
 * Reprezentacja danych pojedynczego węzła w drzewie.
 */
export class ResourceItem {
    /** Identyfikator danych. */
    id: string | null;
    /** Wyświetlana nazwa. */
    name: string; 
    /** Filtr wykorzystywany do załadowania planu zajęć dla zasobu (węzła w drzewie zasobów). */
    filter: Filter | null; 
    /** Dodatkowa ikona wyświetlana obok węzła. */
    icon: string;
    /** Typ danych. */
    type: string | null;
    /** Typ danych dla operacji dodawania (wykorzystywane w drzewie zasobów w panelu administratora). */
    addActionType: string | null;
}

/**
 * Reprezentacja węzła w drzewie.
 */
export class ResourceNode {
    /** Dzieci węzła. */
    children: ResourceNode[];
    /** Reprezentacja danych węzła. */
    item: ResourceItem;
}

/**
 * Reprezentacja węzła w wyprostowanym drzewie.
 */
export class ResourceFlatNode {
    /** Reprezentacja danych węzła. */
    item: ResourceItem;
    /** Poziom węzła. */
    level: number;
    /** Czy posiada możliwość rozwijania (czy posiada dzieci). */
    expandable: boolean;
    /** Czy jest widoczny na ekranie. */
    visible: boolean;
}