// ������ 3.02 �� 23.05.2019�. �������� ������ ��� ���
//
// v3 - ��������� ��������� ���������� ��������� �-����� � ������ IBAN. 
//
using MyTypes;

class ErcExporter
{

	static	int		BFileNum	;
	static  int             SeanceNum	;
	static	CErcConfig	ErcConfig	;
	static	CConnection	Connection1	;
	static	CConnection	Connection2	;
	static	CScrooge2Config	Scrooge2Config	;
	static	CMd5Hash	Md5		=	new CMd5Hash();
	static	CSepAWriter	BFile		=	new CSepAWriter();
	static	readonly string Now_Date_Str	=	CCommon.DtoC(CCommon.Today()).Substring(2, 6);
	static	readonly string Now_Time_Str	=	CCommon.Hour(CCommon.Now()).ToString("00") + CCommon.Minute(CCommon.Now()).ToString("00");

	public	static	bool	WriteBFiles( int DayDate, string DestinationFolder )
	{
		if (DestinationFolder == null)
			return false;

		int FileId;
		CRecordSet Data_Line = new CRecordSet(Connection1);
		CRecordSet Data_Header = new CRecordSet(Connection2);
		string	DebitAcc , CreditAcc
		,	CmdText		=	"exec pMega_OpenGate_Export;2 @TaskCode = 'ErcGate' "
					+	", @Date="	+	DayDate.ToString()
					+	", @SeansNum="	+	BFileNum.ToString()
					+	", @FileName=''"	;
		if (Data_Header.Open(CmdText))
		{
			while (Data_Header.Read())
			{
				FileId = CCommon.CInt32(Data_Header["FileId"]);

				if (Data_Line.Open("exec dbo.pMega_OpenGate_Export;3 @FileId=" + FileId.ToString()))
				{

					if (BFile.Create( DestinationFolder + "\\" + Data_Header["FileName"].Trim(), CAbc.CHARSET_DOS)) {

						BFile.Head[CSepAFileInfo.H_EMPTYSTR]	=	"";				// char[100]  // ��p��� 100 - �p�����
						BFile.Head[CSepAFileInfo.H_CRLF1]	=	CAbc.CRLF;			// char[  2]; // ������ ������ ������
						BFile.Head[CSepAFileInfo.H_FILENAME]	=	Data_Header["FileName"].Trim();	// char[ 12]; // ������������  �����
						BFile.Head[CSepAFileInfo.H_DATE]	=	Now_Date_Str;			// char[  6]; // ���� �������� �����
						BFile.Head[CSepAFileInfo.H_TIME]	=	Now_Time_Str;			// char[  4]; // ���� �������� �����
						BFile.Head[CSepAFileInfo.H_STRCOUNT]	=	Data_Header["TotalLines"].Trim();// char[  6]; // ���������� �� � �����
						BFile.Head[CSepAFileInfo.H_TOTALDEBET]	=	"0";				// char[ 16]; // ����� ������ �� �����
						BFile.Head[CSepAFileInfo.H_TOTALCREDIT]	=	Data_Header["TotalCents"].Trim();// char[ 16]; // ����� �p����� �� �����
						BFile.Head[CSepAFileInfo.H_DES]		=	Md5.GetHash(BFile.GetHeader().Substring(102, 60));	// char[ 64]; // ���
						BFile.Head[CSepAFileInfo.H_DES_ID]	=	"UIAB00";			// char[  6]; // ID ����� ���
						BFile.Head[CSepAFileInfo.H_DES_OF_HEADER]=	"";				// char[ 64]; // ��� ���������
						BFile.Head[CSepAFileInfo.H_CRLF2]	=	CAbc.CRLF;			// char[  2]; // ������ ����� ������

						if ( ! BFile.WriteHeader())
						{
							Data_Header.Close();
							Data_Line.Close();
							BFile.Close();
							return false;
						}

						while (Data_Line.Read())
						{
							DebitAcc=Data_Line["DebitAcc"].Trim();
							CreditAcc=Data_Line["CreditAcc"].Trim();
							BFile.Line[CSepAFileInfo.L_DEBITMFO]	=	Data_Line["DebitMfo"].Trim();				// char[  9]; // �����-���
							BFile.Line[CSepAFileInfo.L_DEBITACC]	=	DebitAcc;						// char[ 14]; // �����-����
							BFile.Line[CSepAFileInfo.L_DEBITACC_EXT]=	( DebitAcc.Length>14 ? DebitAcc : "" );			// char[ 20]; // ����������� �����-����
							BFile.Line[CSepAFileInfo.L_DEBITIBAN]	=	Data_Line["DebitIBAN"].Trim();				// char[ 34]; // �����-IBAN
							BFile.Line[CSepAFileInfo.L_OKPO1]	=	Data_Line["DebitState"].Trim();				// char[ 14]; // �����.��� ������� �
							BFile.Line[CSepAFileInfo.L_DEBITNAME]	=	Data_Line["DebitName"].Trim().Replace("?", "i");	// char[ 38]; // ������������ �����-�����
							BFile.Line[CSepAFileInfo.L_CREDITMFO]	=	Data_Line["CreditMfo"].Trim();				// char[  9]; // ������-���
							BFile.Line[CSepAFileInfo.L_CREDITACC]	=	CreditAcc;						// char[ 14]; // ������ ����
							BFile.Line[CSepAFileInfo.L_CREDITACC_EXT]=	( CreditAcc.Length>14 ? CreditAcc : "" );		// char[ 20]; // ����������� ������ ����
							BFile.Line[CSepAFileInfo.L_OKPO2]	=	Data_Line["CreditState"].Trim();			// char[ 14]; // �����.��� ������� �
							BFile.Line[CSepAFileInfo.L_CREDITIBAN]	=	Data_Line["CreditIBAN"].Trim();				// char[ 34]; // ������-IBAN
							BFile.Line[CSepAFileInfo.L_CREDITNAME]	=	Data_Line["CreditName"].Trim().Replace("?", "i");	// char[ 38]; // ������������ ������-�����
							BFile.Line[CSepAFileInfo.L_FLAG]	=	"1";							// char[  1]; // ���� `�����/������`
							BFile.Line[CSepAFileInfo.L_SUMA]	=	Data_Line["Cents"].Trim();				// char[ 16]; // ����� � ��������
							BFile.Line[CSepAFileInfo.L_DTYPE]	=	"6";							// char[  2]; // ��� ���������
							BFile.Line[CSepAFileInfo.L_NDOC]	=	Data_Line["Code"].Trim();				// char[ 10]; // ����� ���������
							BFile.Line[CSepAFileInfo.L_CURRENCY]	=	Data_Line["CurrencyId"].Trim();				// char[  3]; // ������
							BFile.Line[CSepAFileInfo.L_DATE1]	=	CCommon.DtoC(CCommon.CInt32(Data_Line["DayDate"].Trim())).Substring(2, 6);  // char[  6]; ���� �������
							BFile.Line[CSepAFileInfo.L_DATE2]	=	CCommon.DtoC(CCommon.CInt32(Data_Line["OrgDate"].Trim())).Substring(2, 6);  // char[  6]; ���� ����������� ���������
							BFile.Line[CSepAFileInfo.L_PURPOSE]	=	Data_Line["Purpose"].Trim().Replace("?", "i");		// char[160]; // ���������� �������
							BFile.Line[CSepAFileInfo.L_SYMBOL]	=	Data_Line["Ctrls"].Trim();				// char[  3]; // �������� ������
							BFile.Line[CSepAFileInfo.L_ID]		=	Data_Line["TransferId"].Trim();				// char[  9]; // ������������� ���������
							BFile.Line[CSepAFileInfo.L_DES]		=	Md5.GetHash(BFile.GetFullLine().Substring(0, 444));	// char[ 64]; // ���
							BFile.Line[CSepAFileInfo.L_CRLF]	=	CAbc.CRLF;						// char[  2]; // ������ `����� ������`

							if ( !BFile.WriteLine() )
							{
								Data_Header.Close();
								Data_Line.Close();
								BFile.Close();
								return false;
							}
						}
					}
					BFile.Close();
				}
			}
			Data_Line.Close();
		}
		Data_Header.Close();
		return true;
	}

	public	static	bool	WriteVFiles( int DayDate, string DestinationFolder , int VMode )
	{
		CSepVWriter	A	= new	CSepVWriter();
		string	File_Name	=	"!"
					+	( VMode == 0 ? "V" : "W" )
					+	"UUIA"
					+	CCommon.StrY( CCommon.Month( DayDate ) , 1 )
					+	CCommon.StrY( CCommon.Day( DayDate ) , 1  )
					+	"."
					+	CCommon.Right( "0" + CCommon.Hour( CCommon.Clock() ) , 2 )
					+	CCommon.StrY( CCommon.Minute( CCommon.Clock() ) >> 1 , 1 );

		if	( ! A.Create( DestinationFolder + "\\" + File_Name , CAbc.CHARSET_DOS ) )
			return	false;

		CRecordSet Data		= new CRecordSet( Connection1 );
		if	( Data.Open("exec  dbo.pMega_OpenGate_Report;3   @TaskCode='ErcGate', @DateFrom = " + DayDate.ToString() +" , @Mode = " + VMode.ToString() ) )
		{
			if	( ! Data.Read()  )
			{
				A.Head[CSepVFileInfo.H_EMPTYSTR   ]	=	"";				// char[100]  // ��p��� 100 - �p�����
				A.Head[CSepVFileInfo.H_CRLF1      ]	=	CAbc.CRLF;			// char[  2]; // ������ ������ ������
				A.Head[CSepVFileInfo.H_FILENAME   ]	=	CCommon.Left( File_Name.Trim() , 12 ) ;	// char[ 12]; // ������������  �����
				A.Head[CSepVFileInfo.H_DATE       ]	=	Now_Date_Str;			// char[  6]; // ���� �������� �����
				A.Head[CSepVFileInfo.H_TIME       ]	=	Now_Time_Str;			// char[  4]; // ���� �������� �����
				A.Head[CSepVFileInfo.H_STRCOUNT   ]	=	"0";				// char[  6]; // ���������� �� � �����
				A.Head[CSepVFileInfo.H_TOTALDEBET ]	=	"0";				// char[ 16]; // ����� ������ �� �����
				A.Head[CSepVFileInfo.H_TOTALCREDIT]	=	"0";				// char[ 16]; // ����� �p����� �� �����
				A.Head[CSepVFileInfo.H_EMPTYSTR2  ]	=	"";				// char[ 64]; // ������ ������������
				A.Head[CSepVFileInfo.H_CRLF2      ]	=	CAbc.CRLF;			// char[  2]; // ������ ����� ������
				if	( ! A.WriteHeader() ) {
					Data.Close();
					A.Close();
					return	false;
				}
			}
			else {
				A.Head[CSepVFileInfo.H_EMPTYSTR   ]	=	"";				// char[100]  // ��p��� 100 - �p�����
				A.Head[CSepVFileInfo.H_CRLF1      ]	=	CAbc.CRLF;			// char[  2]; // ������ ������ ������
				A.Head[CSepVFileInfo.H_FILENAME   ]	=	CCommon.Left( File_Name.Trim() , 12 ) ;	// char[ 12]; // ������������  �����
				A.Head[CSepVFileInfo.H_DATE       ]	=	Now_Date_Str;			// char[  6]; // ���� �������� �����
				A.Head[CSepVFileInfo.H_TIME       ]	=	Now_Time_Str;			// char[  4]; // ���� �������� �����
				A.Head[CSepVFileInfo.H_STRCOUNT   ]	=	Data["TotalLines"].Trim();	// char[  6]; // ���������� �� � �����
				A.Head[CSepVFileInfo.H_TOTALDEBET ]	=	"0";				// char[ 16]; // ����� ������ �� �����
				A.Head[CSepVFileInfo.H_TOTALCREDIT]	=	Data["TotalCents"].Trim();	// char[ 16]; // ����� �p����� �� �����
				A.Head[CSepVFileInfo.H_EMPTYSTR2  ]	=	"";				// char[ 64]; // ������ ������������
				A.Head[CSepVFileInfo.H_CRLF2      ]	=	CAbc.CRLF;			// char[  2]; // ������ ����� ������
				if	( ! A.WriteHeader() )
				{
					Data.Close();
					A.Close();
					return	false;
				}
				do
				{
					A.Line[CSepVFileInfo.L_DEBITMFO	]	=	Data["SourceCode"].Trim() ;	// char[  9]; // �����-���
					A.Line[CSepVFileInfo.L_DEBITACC	]	=	Data["DebitMoniker"].Trim() ;	// char[ 14]; // �����-����
					A.Line[CSepVFileInfo.L_CREDITMFO]	=	Data["TargetCode"].Trim();	// char[  9]; // ������-���
					A.Line[CSepVFileInfo.L_CREDITACC]	=	Data["CreditMoniker"].Trim() ;	// char[ 14]; // ������ ����
					A.Line[CSepVFileInfo.L_FLAG	]	=	"1";				// char[  1]; // ���� `�����/������`
					A.Line[CSepVFileInfo.L_SUMA	]	=	Data["CrncyCents"].Trim();	// char[ 16]; // ����� � ��������
					A.Line[CSepVFileInfo.L_DTYPE	]	=	Data["Kind"].Trim();		// char[  2]; // ��� ���������
					A.Line[CSepVFileInfo.L_NDOC	]	=	Data["DocNum"].Trim();		// char[ 10]; // ����� ���������
					A.Line[CSepVFileInfo.L_CURRENCY	]	=	Data["CurrencyId"].Trim();	// char[  3]; // ������
					A.Line[CSepVFileInfo.L_DATE1	]	=	CCommon.DtoC(CCommon.CInt32(Data["DayDate"].Trim())).Substring(2, 6) ;	// char[  6]; // ���� �������
					A.Line[CSepVFileInfo.L_ID	]	=	Data["Id"].Trim();		// char[  9]; // ������������� ���������
					A.Line[CSepVFileInfo.L_FILENAME1]	=	Data["FileName"].Trim();	// char[ 12]; // ��� ����� N 1
					A.Line[CSepVFileInfo.L_LINENUM1	]	=	Data["LineNum"].Trim();		// char[  6]; // ����� ������ � ����� N 1
					A.Line[CSepVFileInfo.L_FILENAME2]	=	Data["FileName"].Trim();	// char[ 12]; // ��� ����� N 2
					A.Line[CSepVFileInfo.L_LINENUM2	]	=	Data["LineNum"].Trim();	// char[  6]; // ����� ������ � ����� N 2
					A.Line[CSepVFileInfo.L_STATUS	]	=	( ( ( CCommon.CInt32(Data["ProcessFlag"].Trim()) & 3 ) == 3 )  ? "Y" : "N" ); // char[  1]; // ���� �������� �������
					A.Line[CSepVFileInfo.L_TIME	]	=	Now_Time_Str;			// char[  4]; // �����
					A.Line[CSepVFileInfo.L_NOL1	]	=	"0";				// char[  1]; //
					A.Line[CSepVFileInfo.L_NOL2	]	=	"0";				// char[  1]; //
					A.Line[CSepVFileInfo.L_CRLF	]	=	CAbc.CRLF;			// char[  2]; // ������� ����� ������
					if	( ! A.WriteLine() ) {
						Data.Close();
						A.Close();
						return	false;
					}
				}	while	( Data.Read() )	;
			}
		}
		Data.Close();
		A.Close();
		return	true;
	}

	public static void Main()
	{
		const	bool	DEBUG		=	false		;
		int		WorkMode	=	0		;// 1 = ��������� B ; 2 = ��������� V
		const	int	WORK_MODE_B	=	1		;// ��� WorkMode : 1 = ��������� B
		const	int	WORK_MODE_V	=	2		;// ��� WorkMode : 2 = ��������� V
		int		DayStatus	=	0		;// &1 = ���� �� B ; &2 = ���� �� V
		int		ErcDate		=	CCommon.Today()	;
		string		TmpDir		=	null		;
		string		StatDir		=	null		;
		string		TodayDir	=	null		;
		string		OutputDir	=	null		;
		string		DataBase	=	null		;
		string		ServerName	=	null		;
		string		ScroogeDir	=	null		;
		string		LogFileName	=	null		;
		string		SimpleFileName	=	null		;
		string		ConfigFileName	=	null		;
		string		ConnectionString=	null		;

		Err.LogToConsole() ;
		CCommon.Print( ""," �������� ������ ��� ���. ������ 3.02 �� 23.05.2019�." ) ;

		if	( ! DEBUG )
		{
	                if	( CCommon.ParamCount() < 2 )
	                {
        	        	CCommon.Print(" ����� ������ ��������� (�������� � ������ ������� ���������):") ;
				CCommon.Print("        /E      -  ��������� �������� ������ ��� ��� .") ;
				CCommon.Print(" ������� ������� ���������  : ");
				CCommon.Print("        ErcExport  /E");
				CCommon.Print("        ErcExport  /E  2019/05/20") ;
				return	;
        	        }
                	if	( CCommon.Upper( CAbc.ParamStr[1] ).Trim()  != "/E" )
                	{
				CCommon.Print(" ������ ! ������������ ����� ������  - " + CAbc.ParamStr[1] );
        	        	return;
			}
			if	( CCommon.ParamCount() > 2 )
			{
				ErcDate	=	CCommon.GetDate( CAbc.ParamStr[2].Trim() );
				if	( ErcDate < 10000 ) {
					CCommon.Print(" ������ ! ������������ ���� - " + CAbc.ParamStr[2] );
					return;
				}
			}
		}
		else
			CCommon.Print("--- DEBUG ---" );
		CCommon.Print(" ������� ���� " +  CCommon.StrD( ErcDate , 10 , 10  ) );

		// - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		Scrooge2Config	= new	CScrooge2Config();
		if (!Scrooge2Config.IsValid)
		{
			CCommon.Print( Scrooge2Config.ErrInfo ) ;
			return;
		}

		ScroogeDir	=	(string)Scrooge2Config["Root"];
		ServerName	=	(string)Scrooge2Config["Server"];
		DataBase	=	(string)Scrooge2Config["DataBase"];
		if( ScroogeDir == null )
		{
			CCommon.Print("  �� ������� ���������� `Root` � ���������� `������-2` ");
			return;
		}
		if( ServerName == null )
		{
			CCommon.Print("  �� ������� ���������� `Server` � ���������� `������-2` ");
			return;
		}
		if( DataBase == null )
		{
			CCommon.Print("  �� ������� ���������� `Database` � ���������� `������-2` ");
			return;
		}
		CCommon.Print("  ���� ��������� `������-2` ����� :  " + ScroogeDir );

		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		ConnectionString	=	"Server="	+	ServerName
					+	";Database="	+	DataBase
					+	";Integrated Security=TRUE;"
					;
		Connection1 = new CConnection(ConnectionString);
		Connection2 = new CConnection(ConnectionString);

		if (Connection1.IsOpen())
		{
			CCommon.Print("  ������        :  " + ServerName  );
			CCommon.Print("  ���� ������   :  " + DataBase + CAbc.CRLF );
		}
		else {
			CCommon.Print( CAbc.CRLF + "  ������ ����������� � ������� !" );
			return;
		}

		// - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		CCommand        Command         = new   CCommand( Connection1 );
		DayStatus	=	( int ) CCommon.IsNull( Command.GetScalar(" exec dbo.pMega_OpenGate_Days;8 @TaskCode = 'ErcGate' , @DayDate = " + ErcDate.ToString() ) , (int) 0 );

		switch	( ( DayStatus & 3 ) )
		{
			case  0:{			// ��������� �������� � B � V
				WorkMode	=	WORK_MODE_B;	// ���������� B
				break;
			}
			case  1:{			// ��������� �������� B ; ��������� �������� V
				WorkMode	=	WORK_MODE_V;	// ���������� V
				break;
			}
			case  2:{			// ��������� �������� B ; ��������� �������� V
				WorkMode	=	WORK_MODE_B;	// ���������� B
				break;
			}
			case  3:{
				CCommon.Print( " �� " + CCommon.StrD( ErcDate , 10 , 10  ) + " �������� ����� B � V ��������� (��. ������� �������� ���)." );
				Connection1.Close();
				Connection2.Close();
				return;
				break;
			}
		}

		SeanceNum	=	( int ) CCommon.IsNull( Command.GetScalar(" exec dbo.pMega_OpenGate_Days;4  @TaskCode = 'ErcGate' , @ParamCode = 'NumSeance'  , @DayDate = " + ErcDate.ToString() ) , (int) 0 );
		if	( WorkMode == WORK_MODE_B )
		{
			BFileNum	=	( int ) CCommon.IsNull( Command.GetScalar(" exec dbo.pMega_OpenGate_Days;4  @TaskCode = 'ErcGate' , @ParamCode = 'NumOutFile' , @DayDate = " + ErcDate.ToString() ) , (int) 0 ) ;
			CCommon.Print(" ����������� ������������ B-������ " ) ;
		}
		else
			CCommon.Print(" ����������� ������������ V � W -������ " ) ;

		Command.Close();
		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		ConfigFileName	=	ScroogeDir + CAbc.SLASH + "EXE" + CAbc.SLASH + "GLOBAL.ERC" ;
		ErcConfig = new CErcConfig();
		ErcConfig.Open( ErcDate );
		if ( !ErcConfig.IsValid() )
		{
			CCommon.Print( "  ������ ������ �������� �� ����� " + ConfigFileName );
			System.Console.WriteLine(ErcConfig.ErrInfo())		;
			Connection1.Close();
			Connection2.Close();
			return;
		}
		TodayDir	=	(string)ErcConfig.TodayDir()		;
		StatDir		=	(string)ErcConfig.StatDir()		;
		TmpDir		=	(string)ErcConfig.TmpDir()		;
		OutputDir	=	(string)ErcConfig["OutputDir"]		;
		if ( (TodayDir == null) || (OutputDir == null) )
		{
			CCommon.Print( "  ������ ������ �������� �� ����� " + ConfigFileName );
			Connection1.Close();
			Connection2.Close();
			return;
		}
		TodayDir	=	TodayDir.Trim() ;
		OutputDir	=	OutputDir.Trim();
		StatDir		=	StatDir.Trim();
		if ( (TodayDir == "")  || (OutputDir == "" )  || (StatDir == "" )  )
		{
			CCommon.Print( "  ������ ������ �������� �� ����� " + ConfigFileName );
			Connection1.Close();
			Connection2.Close();
			return;
		}
		if	( ! CCommon.DirExists(StatDir) )
			CCommon.MkDir(StatDir);
		if	( ! CCommon.SaveText( StatDir + "\\" + "test.dat" , "test.dat" , CAbc.CHARSET_DOS ) )
		{
			CCommon.Print( " ������ ������ � ������� " + StatDir );
			Connection1.Close();
			Connection2.Close();
			return;
		}
		CCommon.DeleteFile(StatDir + "\\" + "test.dat");
		CCommon.Print("  ���� ��������� ����� ����� :  " + ConfigFileName );

		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		TmpDir=TmpDir+"\\"+SeanceNum.ToString("000000");
		CCommon.MkDir(TmpDir);
		if	( ! CCommon.SaveText( TmpDir + "\\" + "test.dat" , "test.dat" , CAbc.CHARSET_DOS ) )
		{
			CCommon.Print( "  ������ ������ � ������� " + TmpDir );
			Connection1.Close();
			Connection2.Close();
		}
		LogFileName=ErcConfig.LogDir()+"\\SE"+SeanceNum.ToString("000000")+".TXT";
		if	( ! CCommon.AppendText( LogFileName  , CCommon.Now() + " , " + CCommon.Upper(CCommon.GetUserName()) + CAbc.CRLF, CAbc.CHARSET_DOS ) )
		{
			CCommon.Print( "  ������ ������ � ���� " + LogFileName );
			Connection1.Close();
			Connection2.Close();
		}

		Err.LogTo( LogFileName );

		// - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	�������� B
		if	( WorkMode == WORK_MODE_B )
			if (! WriteBFiles( ErcDate , TmpDir ) )
				CCommon.Print("  ������ ������ B-������!");

		// - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		//	�������� V
		if	( WorkMode == WORK_MODE_V )
		{
			WriteVFiles( ErcDate , TmpDir , 0 );
			WriteVFiles( ErcDate , TmpDir , 1 );
			Command         = new   CCommand( Connection1 );
			if	( ! Command.Execute(" exec dbo.pMega_OpenGate_Days;9  @TaskCode = 'ErcGate' , @DayDate = " + ErcDate.ToString() ) ) {
				CCommon.Print( "  ������ ��������� ������� �� �������� V-�����!" );
				CCommon.AppendText( LogFileName , "  ������ ��������� ������� �� �������� V-�����!" , CAbc.CHARSET_DOS );
				CCommon.AppendText( LogFileName , CAbc.CRLF , CAbc.CHARSET_DOS );
				Connection1.Close();
				Connection2.Close();
				return;
			}
			Command.Close();
		}

		// - - - - - - - - - - - - - - - - - - - - - - - - - - - - -
		foreach	( string ResultFile in CCommon.GetFileList( TmpDir+ "\\" + "!*.*" ) )
		{
			if ( ResultFile != null )
			{
				SimpleFileName=CCommon.GetFileName( ResultFile );
				CCommon.CopyFile( ResultFile , TodayDir + "\\" + SimpleFileName );
				CCommon.CopyFile( ResultFile , OutputDir + "\\" + SimpleFileName );
				CCommon.AppendText( LogFileName , "��������� ���� " + SimpleFileName , CAbc.CHARSET_DOS );
				CCommon.Print("  ��������� ���� " + SimpleFileName);
			}
		}
		CCommon.AppendText( LogFileName , CAbc.CRLF , CAbc.CHARSET_DOS );
		Connection1.Close();
		Connection2.Close();
	}
}