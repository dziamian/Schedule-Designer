import { Action, createReducer, on } from "@ngrx/store";
import { Account, Titles, User } from "../others/Accounts";
import { setAccount } from "./account.actions";

export const account : Account = new Account(new User(0, "", ""), null, null, null);

const _accountReducer = createReducer(
    account,
    on(setAccount, (state, {account}) => account)
);

export function accountReducer(state : Account | undefined, action : Action) {
    return _accountReducer(state, action);
}