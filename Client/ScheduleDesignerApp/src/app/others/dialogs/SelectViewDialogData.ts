import { Settings } from "../Settings";

export class SelectViewDialogData {
    constructor(
        public Settings: Settings
    ) {}
}

export class SelectViewDialogResult {
    static readonly EMPTY:SelectViewDialogResult 
        = new SelectViewDialogResult(
            []
        );

    constructor(
        public SelectedWeeks: number[]
    ) {}
}