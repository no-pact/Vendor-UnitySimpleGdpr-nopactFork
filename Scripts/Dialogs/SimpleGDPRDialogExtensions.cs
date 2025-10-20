// Copyright © 2024 no-pact

using System.Collections;
    
public static class SimpleGDPRDialogExtensions
{
    public static void ShowDialog(this IGDPRDialog dialog, SimpleGDPR.DialogClosedDelegate onDialogClosed = null)
    {
        dialog.ShowDialog( onDialogClosed );
    }

    public static IEnumerator WaitForDialog(this IGDPRDialog dialog)
    {
        dialog.ShowDialog( null );

        while( SimpleGDPR.IsDialogVisible )
            yield return null;
    }
}