import { Action, createReducer, on } from "@ngrx/store";
import { Account, Titles } from "../others/Accounts";
import { setAccount } from "./account.actions";

export const account : Account = new Account(0, "", "", false, [], false, new Titles("", ""), false, false);

const _accountReducer = createReducer(
    account,
    on(setAccount, (state, {account}) => account)
);

export function accountReducer(state : Account | undefined, action : Action) {
    return _accountReducer(state, action);
}